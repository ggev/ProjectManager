﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManager.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly ProjectDBContext _context;

        public EmployeesController(ProjectDBContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var employeeList = await _context.Employees.ToListAsync();
            var employmentDictionary = new Dictionary<int, short>();

            foreach (var item in employeeList)
            {
                employmentDictionary.Add(item.EmployeeId, GetEmploymentPercent(item.EmployeeId));
            }
            ViewBag.Employment = employmentDictionary;
            return View(employeeList);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var employee = await _context.Employees
                .SingleOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }
            if (employee.Photo != null)
            {
                var base64 = Convert.ToBase64String(employee.Photo);
                ViewBag.ImgSrc = String.Format("data:image/gif;base64,{0}", base64);
            }

            ViewBag.Experiance = TotalExperiance(employee);
            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,FirstName,LastName,Birthday,PhoneNumber,Position,Salary,Experiance,BeginningOfWork,Image,Description")] Employees employees)
        {
            if (ModelState.IsValid)
            {
                employees.CalculateMonthPaymentFrom = employees.BeginningOfWork;
                employees.Vacation = 0;
                employees.Overtime = 0;
                UploadPhoto(employees);
                _context.Add(employees);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(employees);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employees = await _context.Employees.SingleOrDefaultAsync(m => m.EmployeeId == id);
            await CalculateCurrentPayment((int)id);
            if (employees == null)
            {
                return NotFound();
            }

            return View(employees);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,FirstName,LastName,Birthday,PhoneNumber,Position,Salary,Experiance,BeginningOfWork,CalculateMonthPaymentFrom,Image,Description")] Employees employees)
        {
            if (id != employees.EmployeeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    UploadPhoto(employees);
                    _context.Update(employees);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeesExists(employees.EmployeeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employees);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employees = await _context.Employees
                .SingleOrDefaultAsync(m => m.EmployeeId == id);
            if (employees == null)
            {
                return NotFound();
            }

            return View(employees);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employees = await _context.Employees.SingleOrDefaultAsync(m => m.EmployeeId == id);
            var employeeFromProjectEmployees = from p in _context.ProjectEmployees
                                               where p.EmployeeId == id
                                               select p;

            foreach (var emp in employeeFromProjectEmployees)
            {
                _context.ProjectEmployees.Remove(emp);
            }
            _context.Employees.Remove(employees);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/SendOnVacation/5
        public async Task<IActionResult> SendOnVacation(int? id)
        {
            var employee = await (from e in _context.Employees
                                  where e.EmployeeId == id
                                  select e).SingleOrDefaultAsync();
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        public JsonResult ProjectsInTheSpecifiedPeriod(int employeeId, DateTime dateFrom, DateTime dateTo)
        {
            var employeeReturnDay = (from e in _context.Employees
                                     where e.EmployeeId == employeeId
                                     select e.ReturnFromVacation).SingleOrDefault();
            if (DateTime.Today.CompareTo(employeeReturnDay) < 0)
            {
                return Json(JsonConvert.SerializeObject(1));
            }
            if (dateFrom == DateTime.MinValue || dateTo == DateTime.MinValue || dateFrom.CompareTo(DateTime.Today) < 0)
            {
                return Json(JsonConvert.SerializeObject(-1));
            }
            var projects = (from p in _context.ProjectEmployees
                            where p.EmployeeId == employeeId
                            select p.Project).ToList();
            if (!projects.Any())
            {
                return Json(JsonConvert.SerializeObject(0));
            }
            var projectList = new List<Projects>();
            DateTime maxDate = dateFrom;
            foreach (var item in projects)
            {
                if (dateFrom.CompareTo(item.Deadline) <= 0)
                {
                    projectList.Add(item);
                    if (maxDate.CompareTo(item.Deadline) < 0)
                    {
                        maxDate = item.Deadline;
                    }
                }
            }
            if (!projectList.Any())
            {
                return Json(JsonConvert.SerializeObject(0));
            }
            return Json(JsonConvert.SerializeObject(projectList));
        }

        public bool CommitVacation(int employeeId, DateTime goToVacationDay, DateTime returnFromVacationDay)
        {
            int freeDays, busyDays;
            var employee = (from e in _context.Employees
                            where e.EmployeeId == employeeId
                            select e).SingleOrDefault();
            var projectsDeadLine = (from p in _context.ProjectEmployees
                                    where p.EmployeeId == employeeId
                                    select p.Project.Deadline).ToArray();
            DateTime maxDate = goToVacationDay;
            if (projectsDeadLine.Any())
            {
                foreach (var item in projectsDeadLine)
                {
                    if (maxDate.CompareTo(item) < 0)
                    {
                        maxDate = item;
                    }
                }
                freeDays = maxDate.DayOfYear - goToVacationDay.DayOfYear + 1;
                busyDays = returnFromVacationDay.DayOfYear - maxDate.DayOfYear;
            }
            freeDays = returnFromVacationDay.DayOfYear - goToVacationDay.DayOfYear + 1;
            busyDays = 0;
            int vacationQuantity = 20 - employee.Vacation;
            if (vacationQuantity <= 0)
            {
                employee.VacationCount = (byte)(freeDays + busyDays);
            }
            else
            {
                if (freeDays >= vacationQuantity)
                {
                    employee.VacationCount = (byte)(freeDays - vacationQuantity);
                    employee.Vacation += (byte)(freeDays + busyDays);
                }
                else
                {
                    employee.VacationCount = (byte)busyDays;
                    employee.Vacation += (byte)(freeDays + busyDays);
                }
            }
            employee.ReturnFromVacation = returnFromVacationDay;
            try
            {
                _context.Update(employee);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // GET: Employees/SetOvertime/5
        public async Task<IActionResult> SetOvertime(int? id)
        {
            var employee = await (from e in _context.Employees
                                  where e.EmployeeId == id
                                  select e).SingleOrDefaultAsync();
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employees/SetOvertime/5
        [HttpPost]
        public async Task<IActionResult> SetOvertime(int id, [Bind("Overtime")] Employees employees)
        {
            var employee = await (from e in _context.Employees
                                  where e.EmployeeId == id
                                  select e).SingleOrDefaultAsync();
            if (employee == null)
            {
                return NotFound();
            }
            employee.Overtime = employees.Overtime;
            try
            {
                await CalculateCurrentPayment(id);
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // get checked employees transaction list
        public async Task<IActionResult> Transaction(int id)
        {
            await CalculateMonthlyPayment(id);
            var transaction = await (from t in _context.Transaction
                                     where t.EmployeeId == id && t.Payment != 0
                                     select t).ToListAsync();
            return View(transaction);
        }

        private bool EmployeesExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        // get employees full work total experiance
        public string TotalExperiance(Employees employees)
        {
            int experiance = (DateTime.Today.Month - employees.BeginningOfWork.Month) + (DateTime.Today.Year - employees.BeginningOfWork.Year) * 12;

            int totalExperinace = employees.Experiance + experiance;

            int year = totalExperinace / 12;
            int month = totalExperinace % 12;

            return $"{year}y {month}m";
        }

        // upload photo and cast to array of bytes
        private void UploadPhoto(Employees employees)
        {
            if (employees.Image != null)
            {
                byte[] imageData = null;
                // read the uploaded image into an array of bytes
                using (var binaryReader = new BinaryReader(employees.Image.OpenReadStream()))
                {
                    imageData = binaryReader.ReadBytes((int)employees.Image.Length);
                    employees.Photo = imageData;
                }
            }
        }

        private short GetEmploymentPercent(int id)
        {
            short sum = 0;
            var percentList = from p in _context.ProjectEmployees
                              where p.EmployeeId == id
                              select p.Percent;
            foreach (var item in percentList)
                sum += item;
            return sum;
        }

        public async Task ChangeTransactionStatus(int transactionId)
        {
            var transaction = await (from t in _context.Transaction
                                     where t.Id == transactionId
                                     select t).SingleOrDefaultAsync();
            if (transaction != null)
            {
                transaction.Status = (byte)(1 - transaction.Status);
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }
        }

        //TODO not working days
        private async Task CalculateMonthlyPayment(Employees employee)
        {
            while (DateTime.Today.CompareTo(employee.CalculateMonthPaymentFrom) > 0 && DateTime.Today.Month != employee.CalculateMonthPaymentFrom.Month)
            {
                DateTime startingDate = employee.CalculateMonthPaymentFrom;
                DateTime today = DateTime.Today;
                byte count = 0;
                int lastDayInMonth = DateTime.DaysInMonth(startingDate.Year, startingDate.Month);
                for (int i = startingDate.Day; i <= lastDayInMonth; i++)
                {
                    DateTime currentDate = startingDate.AddDays(i - startingDate.Day);
                    switch (currentDate.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            break;
                        case DayOfWeek.Saturday:
                            break;
                        default:
                            count++;
                            break;
                    }
                }
                CalculatePaymentDays(employee.ReturnFromVacation, count, employee.VacationCount);
                Transaction transaction = await (from t in _context.Transaction
                                                 where t.EmployeeId == employee.EmployeeId && t.Payment == 0
                                                 select t).SingleOrDefaultAsync();
                if (transaction == null)
                {
                    transaction = new Transaction
                    {
                        EmployeeId = employee.EmployeeId,
                        Date = new DateTime(startingDate.Year, startingDate.Month, lastDayInMonth),
                        PaymentCounter = 0,
                        Status = 0,
                        Payment = count * (8 + employee.Overtime) * employee.Salary
                    };
                    _context.Add(transaction);
                    var tempDate = startingDate.AddMonths(1);
                    employee.CalculateMonthPaymentFrom = new DateTime(tempDate.Year, tempDate.Month, 1);
                    _context.Update(employee);
                }
                else
                {
                    transaction.Date = new DateTime(startingDate.Year, startingDate.Month, lastDayInMonth);
                    transaction.Payment = count * (8 + employee.Overtime) * employee.Salary + transaction.PaymentCounter;
                    transaction.PaymentCounter = 0;
                    _context.Update(transaction);
                    employee.CalculateMonthPaymentFrom = new DateTime(today.Year, today.Month, 1);
                    _context.Update(employee);
                }
                await _context.SaveChangesAsync();
            }
        }


        private async Task CalculateMonthlyPayment(int id)
        {
            var employee = await (from e in _context.Employees
                                  where e.EmployeeId == id
                                  select e).SingleOrDefaultAsync();
            while (DateTime.Today.CompareTo(employee.CalculateMonthPaymentFrom) > 0 && DateTime.Today.Month != employee.CalculateMonthPaymentFrom.Month)
            {
                DateTime startingDate = employee.CalculateMonthPaymentFrom;
                DateTime today = DateTime.Today;
                byte count = 0;
                int lastDayInMonth = DateTime.DaysInMonth(startingDate.Year, startingDate.Month);
                for (int i = startingDate.Day; i <= lastDayInMonth; i++)
                {
                    DateTime currentDate = startingDate.AddDays(i - startingDate.Day);
                    switch (currentDate.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            break;
                        case DayOfWeek.Saturday:
                            break;
                        default:
                            count++;
                            break;
                    }
                }
                CalculatePaymentDays(employee.ReturnFromVacation, count, employee.VacationCount);
                Transaction transaction = await (from t in _context.Transaction
                                                 where t.EmployeeId == employee.EmployeeId && t.Payment == 0
                                                 select t).SingleOrDefaultAsync();
                if (transaction == null)
                {
                    transaction = new Transaction
                    {
                        EmployeeId = employee.EmployeeId,
                        Date = new DateTime(startingDate.Year, startingDate.Month, lastDayInMonth),
                        PaymentCounter = 0,
                        Status = 0,
                        Payment = count * (8 + employee.Overtime) * employee.Salary
                    };
                    _context.Add(transaction);
                    var tempDate = startingDate.AddMonths(1);
                    employee.CalculateMonthPaymentFrom = new DateTime(tempDate.Year, tempDate.Month, 1);
                    _context.Update(employee);
                }
                else
                {
                    transaction.Date = new DateTime(startingDate.Year, startingDate.Month, lastDayInMonth);
                    transaction.Payment = count * (8 + employee.Overtime) * employee.Salary + transaction.PaymentCounter;
                    transaction.PaymentCounter = 0;
                    _context.Update(transaction);
                    employee.CalculateMonthPaymentFrom = new DateTime(today.Year, today.Month, 1);
                    _context.Update(employee);
                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task CalculateCurrentPayment(int id)
        {
            var employee = await (from e in _context.Employees
                                  where e.EmployeeId == id
                                  select e).SingleAsync();
            await CalculateMonthlyPayment(employee);
            byte count = 0;
            DateTime startingDate = employee.CalculateMonthPaymentFrom;
            DateTime today = DateTime.Today;
            for (int i = startingDate.Day; i < DateTime.Today.Day; i++)
            {
                DateTime currentDate = startingDate.AddDays(i - startingDate.Day);
                switch (currentDate.DayOfWeek)
                {
                    case DayOfWeek.Saturday:
                        break;
                    case DayOfWeek.Sunday:
                        break;
                    default:
                        count++;
                        break;
                }
            }
            Transaction transaction = await (from t in _context.Transaction
                                             where t.EmployeeId == employee.EmployeeId && t.Payment == 0
                                             select t).SingleOrDefaultAsync();
            if (transaction == null)
            {
                transaction = new Transaction
                {
                    EmployeeId = employee.EmployeeId,
                    PaymentCounter = count * (8 + employee.Overtime) * employee.Salary,
                    Status = 0,
                    Payment = 0
                };
                _context.Add(transaction);
                employee.CalculateMonthPaymentFrom = today;
                _context.Update(employee);
            }
            else
            {
                transaction.PaymentCounter += count * (8 + employee.Overtime) * employee.Salary;
                _context.Update(transaction);
                employee.CalculateMonthPaymentFrom = today;
                _context.Update(employee);
            }
            await _context.SaveChangesAsync();
        }

        private async Task CalculateCurrentPayment(Employees employee, int vacation)
        {
            await CalculateMonthlyPayment(employee);
            byte count = 0;
            DateTime startingDate = employee.CalculateMonthPaymentFrom;
            DateTime today = DateTime.Today;
            for (int i = startingDate.Day; i < DateTime.Today.Day; i++)
            {
                DateTime currentDate = startingDate.AddDays(i - startingDate.Day);
                switch (currentDate.DayOfWeek)
                {
                    case DayOfWeek.Saturday:
                        break;
                    case DayOfWeek.Sunday:
                        break;
                    default:
                        count++;
                        break;
                }
            }
            Transaction transaction = await (from t in _context.Transaction
                                             where t.EmployeeId == employee.EmployeeId && t.Payment == 0
                                             select t).SingleOrDefaultAsync();
            if (transaction == null)
            {
                transaction = new Transaction
                {
                    EmployeeId = employee.EmployeeId,
                    PaymentCounter = count * (8 + employee.Overtime) * employee.Salary,
                    Status = 0,
                    Payment = 0
                };
                _context.Add(transaction);
                employee.CalculateMonthPaymentFrom = today;
                _context.Update(employee);
            }
            else
            {
                transaction.PaymentCounter += count * (8 + employee.Overtime) * employee.Salary;
                _context.Update(transaction);
                employee.CalculateMonthPaymentFrom = today;
                _context.Update(employee);
            }
            if (employee.Vacation >= 20)
            {
                var dateMark = employee.CalculateMonthPaymentFrom.AddDays(vacation);
                employee.CalculateMonthPaymentFrom = dateMark;
                _context.Update(employee);
                if (dateMark.Month != DateTime.Today.Month)
                {
                    transaction.Payment = transaction.PaymentCounter;
                    transaction.Date = today;
                    transaction.PaymentCounter = 0;
                    _context.Update(transaction);
                }
            }
            else if (employee.Vacation + vacation > 20)
            {
                for (int i = employee.CalculateMonthPaymentFrom.Day; i < today.Day + employee.Vacation + vacation - 20; i++)
                {
                    DateTime currentDate = startingDate.AddDays(i - startingDate.Day);
                    switch (currentDate.DayOfWeek)
                    {
                        case DayOfWeek.Saturday:
                            break;
                        case DayOfWeek.Sunday:
                            break;
                        default:
                            count++;
                            break;
                    }
                }
                var dateMark = employee.CalculateMonthPaymentFrom.AddDays(vacation);
                employee.CalculateMonthPaymentFrom = dateMark;
                _context.Update(employee);
                if (dateMark.Month != DateTime.Today.Month)
                {
                    transaction.Payment = transaction.PaymentCounter;
                    transaction.Date = today;
                    transaction.PaymentCounter = 0;
                    _context.Update(transaction);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="returnFromVacation">A day when employee will back from vacation</param>
        /// <param name="count">Not working days count</param>
        /// <param name="vacationCount">Employees vacation count</param>
        private void CalculatePaymentDays(DateTime returnFromVacation, byte count, byte vacationCount)
        {
            if (DateTime.Today.CompareTo(returnFromVacation) > 0)
            {
                if (count >= vacationCount)
                {
                    count -= vacationCount;
                    vacationCount = 0;
                }
                else
                {
                    vacationCount -= count;
                    count = 0;
                }

            }
        }
    }
}
