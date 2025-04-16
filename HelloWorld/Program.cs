// See https://aka.ms/new-console-template for more information

for (int i = 0; i < 3; i++)
{
    Console.WriteLine("Please enter your name:");
    var name = Console.ReadLine();
    Console.WriteLine($"Hello, {name}");
    Thread.Sleep(1000);
}