using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[InterfaceType("InterfaceThatNeverGetsUsed")]
public interface IInterfaceThatNeverGetsUsed
{
    public int Id { get; set; }
}

[InterfaceType("IPerson")]
public interface IPerson
{
    public int Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
}

public class Person : IPerson
{
    public int Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public static IPerson Create() => new Person()
    {
        Id = 1,
        FirstName = "John",
        LastName = "Smith",
    };
}

public interface IFigure
{
    public int? Id { get; set; }
    
    float Perimeter { get; }

    IPerson? Creator { get; set; }
}

public class Point : IFigure
{
    [GraphQLType("Int!")]
    public int? Id { get; set; }
    
    public float X { get; set; }
    public float Y { get; set; }

    public float Perimeter => 0;

    public IPerson Creator { get; set; }
}

public class Square : IFigure
{
    public int? Id { get; set; }
    
    public Point TopLeft { get; set; }

    public Point BottomRight { get; set; }

    public float Perimeter => Math.Abs(TopLeft.Y - BottomRight.Y) * 2 + Math.Abs(BottomRight.Y - TopLeft.Y) * 2;

    public IPerson? Creator { get; set; }
}

public class Circle : IFigure
{
    
    public int? Id { get; set; }
    
    public Point Center { get; set; }

    public float Radius { get; set; }

    public float Perimeter => (float)Math.PI * 2 * Radius;

    public IPerson? Creator { get; set; }
}

[ExtendObjectType(typeof(Query))]
public class InterfacesExtensions
{
    public IFigure[] GetFigures()
    {
        return GetCircles().Skip(1).Take(1).Concat(
                GetSquares().Skip(1).Take(1).OfType<IFigure>())
            .ToArray();
    }

    public Circle[] GetCircles()
    {
        return Enumerable
            .Range(0, 10)
            .Select(o => new Circle
            {
                Center = new Point { X = o, Y = o },
                Radius = o,
                Creator = Person.Create(),
            })
            .ToArray();
    }

    public Square[] GetSquares()
    {
        return Enumerable
            .Range(0, 10)
            .Select(o => new Square
            {
                TopLeft = new Point { X = o, Y = o },
                BottomRight = new Point { X = o + 10, Y = o + 10 },
                Creator = Person.Create(),
            })
            .ToArray();
    }
}