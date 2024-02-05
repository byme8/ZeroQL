using ZeroQL.TestServer.Query.Models;

namespace ZeroQL.TestServer.Query;

[UnionType("PostContent")]
public interface IPostContent
{
    public int Id { get; set; }

    public User Author { get; set; }
}

public class TextContent : IPostContent
{
    public int Id { get; set; }

    public string Text { get; set; }

    public User Author { get; set; }
}

public class ImageContent : IPostContent
{
    public int Id { get; set; }

    public string ImageUrl { get; set; }

    public ImageResolution Resolution { get; set; }

    public User Author { get; set; }
}

public enum ImageResolution
{
    _1360x720,
    _1920x1080,
    _2560x1440,
    _3840x2160
}


public class FigureContent : IPostContent
{
    public int Id { get; set; }

    public IFigure Figure { get; set; }

    public User Author { get; set; }
}

[QueryType]
public class UnionExtensions
{
    public IPostContent[] GetPosts()
    {
        var postContents = new IPostContent[]
        {
            GetImage(),
            GetText(),
            GetFigure()
        };

        return postContents;
    }

    public ImageContent GetImage() =>
        new()
        {
            Id = 1,
            Author = User.Create(),
            ImageUrl = "http://example.com/image.png",
            Resolution = ImageResolution._3840x2160 
        };

    public TextContent GetText() =>
        new()
        {
            Id = 2,
            Author = User.Create(),
            Text = "Hello World!"
        };

    public FigureContent GetFigure() =>
        new()
        {
            Id = 3,
            Author = User.Create(),
            Figure = new Circle()
            {
                Center = new Point()
                {
                    X = 1,
                    Y = 1
                },
                Radius = 5
            }
        };
}