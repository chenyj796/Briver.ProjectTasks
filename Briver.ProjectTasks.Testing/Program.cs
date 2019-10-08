using System;

namespace Briver.ProjectTasks.Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = new AssemblyInformationTask
            {
                ProjectDir = @"C:\Users\Administrator\Desktop\AAA\WpfApp1\ClassLibrary3\",
                AssemblyFile = @"C:\Users\Administrator\Desktop\AAA\WpfApp1\ClassLibrary3\bin\Debug\ClassLibrary3.dll",
                AssemblyName = "ClassLibrary3",
                ProjectName = "ClassLibrary3",

                Logging = false,
            };
            task.Execute();
        }
    }
}
