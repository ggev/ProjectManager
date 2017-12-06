using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectManager.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ProjectDBContext _context;

        public ProjectsController(ProjectDBContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projectList = await _context.Projects.ToListAsync();
            var projectCostsBenefitList = new Dictionary<Projects, Tuple<double, double>>();
            foreach (var item in projectList)
            {
                projectCostsBenefitList.Add(item, Benefit(item));
            }
            ViewBag.CostsBenefitList = projectCostsBenefitList;
            return View(projectList);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projects = await _context.Projects
                .SingleOrDefaultAsync(m => m.ProjectId == id);
            if (projects == null)
            {
                return NotFound();
            }
            return View(projects);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,Name,Start,Deadline,Budget,Deposit,Status")] Projects projects)
        {
            if (ModelState.IsValid)
            {
                _context.Add(projects);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(projects);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projects = await _context.Projects.SingleOrDefaultAsync(m => m.ProjectId == id);
            if (projects == null)
            {
                return NotFound();
            }
            return View(projects);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,Name,Start,Deadline,Budget,Deposit,Status")] Projects projects)
        {
            if (id != projects.ProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(projects);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectsExists(projects.ProjectId))
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
            return View(projects);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projects = await _context.Projects
                .SingleOrDefaultAsync(m => m.ProjectId == id);
            if (projects == null)
            {
                return NotFound();
            }
            return View(projects);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var projectEmployees = from p in _context.ProjectEmployees
                                   where p.ProjectId == id
                                   select p;
            foreach (var item in projectEmployees)
            {
                _context.ProjectEmployees.Remove(item);
            }

            var projects = await _context.Projects.SingleOrDefaultAsync(m => m.ProjectId == id);
            _context.Projects.Remove(projects);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/AddEmployee
        public async Task<IActionResult> AddEmployee(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var projectName = await (from p in _context.Projects
                                     where p.ProjectId == id
                                     select p.Name).FirstOrDefaultAsync();
            ViewBag.ProjectId = id;
            ViewBag.ProjectName = projectName;
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName");
            return View();
        }

        // POST: Projects/AddEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee([Bind("ProjectEmployeesId,ProjectId,EmployeeId,Percent")] ProjectEmployees projectEmployees, int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                projectEmployees.ProjectId = (int)id;
                if (!CheckEmployeeExist(projectEmployees.EmployeeId, id))
                {
                    _context.Add(projectEmployees);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Projects");
                }
                else
                {
                    return RedirectToAction(nameof(AddEmployee), new { id = id });
                }
            }
            var projectName = await (from p in _context.Projects
                                     where p.ProjectId == id
                                     select p.Name).FirstOrDefaultAsync();

            ViewBag.ProjectName = projectName;
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "FullName", projectEmployees.EmployeeId);
            return View();
        }

        public async Task<ActionResult> ChangePercent(int id)
        {
            var item = await (from p in _context.ProjectEmployees.Include(p => p.Project).Include(e => e.Employee)
                              where p.ProjectEmployeesId == id
                              select p).FirstOrDefaultAsync();
            var percent = GetEmploymentPercent(item.EmployeeId);
            ViewBag.Inactivity = 100 - percent;
            return View(item);
        }

        [HttpPost]
        public async Task<ActionResult> ChangePercent(int id, short percent)
        {
            if (ModelState.IsValid)
            {
                var project = await (from p in _context.ProjectEmployees
                                     where p.ProjectEmployeesId == id
                                     select p).FirstOrDefaultAsync();
                project.Percent = percent;

                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectEmployeesExists(project.ProjectEmployeesId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                var empoyeeId = await (from p in _context.ProjectEmployees
                                       where p.ProjectEmployeesId == id
                                       select p.EmployeeId).FirstOrDefaultAsync();
                return RedirectToAction(nameof(EmployeeProjects), new { id = empoyeeId });
            }
            return View();
        }

        public ActionResult EmployeeProjects(int id)
        {
            var List = from p in _context.ProjectEmployees
                       where p.EmployeeId == id
                       select p;
            return View(List.Include(p => p.Project).Include(e => e.Employee));
        }

        //Retuns the list of employees of the current project
        public async Task<ActionResult> Employees(int id)
        {
            var employeeList = await (from e in _context.ProjectEmployees.Include(p => p.Employee)
                                      where e.ProjectId == id
                                      select e.Employee).ToListAsync();
            var employmentDictionary = new Dictionary<int, short>();

            foreach (var item in employeeList)
            {
                employmentDictionary.Add(item.EmployeeId, GetEmploymentPercent(item.EmployeeId));
            }
            ViewBag.Employment = employmentDictionary;
            ViewBag.ProjectName = await (from p in _context.Projects
                                         where p.ProjectId == id
                                         select p.Name).SingleOrDefaultAsync();
            ViewBag.ProjectId = id;
            return View(employeeList);
        }

        private Tuple<double, double> Benefit(Projects project)
        {
            double cost = 0;
            int workTerm = 365 * (project.Deadline.Year - project.Start.Year) + 30 * (project.Deadline.Month - project.Start.Month) + project.Deadline.Day - project.Start.Day;

            var list = from p in _context.ProjectEmployees
                       where p.ProjectId == project.ProjectId
                       select p;
            foreach (var p in list.Include(e => e.Employee))
            {
                cost += (p.Employee.Salary * p.Percent * workTerm / 300);
            }
            var costsBenefit = new Tuple<double, double>(cost, project.Budget - cost);
            return costsBenefit;
        }

        private bool ProjectsExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }

        private bool ProjectEmployeesExists(int id)
        {
            return _context.ProjectEmployees.Any(e => e.ProjectEmployeesId == id);
        }

        public short GetEmploymentPercent(int id)
        {
            short sum = 0;

            var percentList = from p in _context.ProjectEmployees
                              where p.EmployeeId == id
                              select p.Percent;
            foreach (var item in percentList)
                sum += item;
            return sum;
        }

        public bool CheckEmployeeExist(int employeeId, int? projectId)
        {
            return (from p in _context.ProjectEmployees
                    where p.ProjectId == projectId && p.EmployeeId == employeeId
                    select p).Any();
        }

        /// <summary>
        /// Returns the current employee's percent of employment in project 
        /// </summary>
        /// <param name="project">Model</param>
        /// <param name="id">Current employee id</param>
        /// <returns></returns>
        private short GetPercentValue(Projects project, int id)
        {
            var percent = (from p in _context.ProjectEmployees
                           where p.EmployeeId == id && p.Project == project
                           select p.Percent).FirstOrDefault();
            return percent;
        }
    }
}
