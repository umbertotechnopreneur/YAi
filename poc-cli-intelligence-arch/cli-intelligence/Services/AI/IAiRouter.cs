namespace cli_intelligence.Services.AI;

interface IAiRouter
{
    IAiClient Resolve(AiRequestContext context);
}
