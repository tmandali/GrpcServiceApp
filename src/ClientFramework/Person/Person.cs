namespace ClientFramework.Person;

public abstract class Person 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  
};

public class SoftwareArchitech : Person
{
}

public class SoftwareDeveloper : Person
{
}