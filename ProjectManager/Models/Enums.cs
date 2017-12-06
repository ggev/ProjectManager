namespace ProjectManager.Models
{
    public enum EmployeePosition
    {
        AspDeveloper = 1,
        JavascriptDeveloper,
        WebDesigner,
        IOSDeveloper,
        AndroidDeveloper
    }

    public enum ProjectStatus
    {
        Todo = 1,
        InProgress,
        Done
    }

    public enum TransactionStatus
    {
        Unpaid,
        Paid
    }
}
