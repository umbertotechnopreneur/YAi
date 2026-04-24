namespace cli_intelligence.Services.AI;

sealed class AiRouter : IAiRouter
{
    private readonly IAiClient _localClient;
    private readonly IAiClient _frontierClient;
    private readonly IAiRoutingPolicy _policy;

    public AiRouter(
        IAiClient localClient,
        IAiClient frontierClient,
        IAiRoutingPolicy policy)
    {
        _localClient = localClient;
        _frontierClient = frontierClient;
        _policy = policy;
    }

    public IAiClient Resolve(AiRequestContext context)
    {
        return _policy.Decide(context) switch
        {
            AiBackend.Local => _localClient,
            _               => _frontierClient
        };
    }
}
