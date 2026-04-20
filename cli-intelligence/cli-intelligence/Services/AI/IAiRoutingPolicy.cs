namespace cli_intelligence.Services.AI;

enum AiBackend
{
    Local,
    Frontier
}

interface IAiRoutingPolicy
{
    AiBackend Decide(AiRequestContext context);
}
