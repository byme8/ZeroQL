namespace ZeroQL.TestServer.Query;

[UnionType("PostContent")]
public interface IPostContent
{
}

public class TextContent : IPostContent
{
    public string Text { get; set; }
}

public class ImageContent : IPostContent
{
    public string ImageUrl { get; set; }

    public int Height { get; set; }
}

[ExtendObjectType(typeof(Query))]
public class UnionExtensions
{
    public IPostContent[] GetPosts()
    {
        return new IPostContent[]
        {
            GetImage(),
            GetText()
        };
    }
    
    public ImageContent GetImage()
    {
        return new ImageContent()
        {
            ImageUrl = "http://example.com/image.png",
            Height = 1920
        };
    }
    
    public TextContent GetText()
    {
        return new TextContent()
        {
            Text = "Hello World!"
        };
    }
}