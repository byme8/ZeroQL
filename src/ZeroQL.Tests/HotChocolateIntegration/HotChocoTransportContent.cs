namespace ZeroQL.Tests.HotChocolateIntegration;

public class HotChocoTransportContent : IGraphQLTransportContent
{
	public HotChocoTransportContent(string graphqlRequest)
	{
		GraphQLRequest = !string.IsNullOrEmpty(graphqlRequest) ? graphqlRequest : throw new ArgumentNullException(nameof(graphqlRequest));
	}

	public string GraphQLRequest { get; }
}
