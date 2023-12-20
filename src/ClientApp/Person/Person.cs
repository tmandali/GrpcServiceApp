namespace ClientApp.Person;

public abstract record class Person (int Id, string Name);

public record SoftwareArchitech(int Id, string Name) : Person(Id, Name);

public record SoftwareDeveloper(int Id, string Name) : Person(Id, Name);
