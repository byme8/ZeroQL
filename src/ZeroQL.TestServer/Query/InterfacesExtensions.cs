using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

public interface IFigure
{
    float Perimeter { get; }
    
    User Creator { get; set; }
}

public class Point : IFigure
{
    public float X { get; set; }
    public float Y { get; set; }

    public float Perimeter => 0;
    
    public User Creator { get; set; }
}

public class Square : IFigure
{
    public Point TopLeft { get; set; }

    public Point BottomRight { get; set; }
    
    public float Perimeter => Math.Abs(TopLeft.Y - BottomRight.Y) * 2 + Math.Abs(BottomRight.Y - TopLeft.Y) * 2;

    public User Creator { get; set; }
}

public class Circle : IFigure
{
    public Point Center { get; set; }

    public float Radius { get; set; }
    
    public float Perimeter => (float)Math.PI * 2 * Radius;

    public User Creator { get; set; }
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
                Creator = User.Create(),
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
                Creator = User.Create(),
            })
            .ToArray();
    }
}