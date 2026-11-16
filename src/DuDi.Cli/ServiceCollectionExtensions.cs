using DuDi.Agent;
using DuDi.Agent.Tools;
using DuDi.Core.Registry;
using DuDi.Core.Risk;
using DuDi.Core.Risk.Rules;
using DuDi.Core.Verification;
using DuDi.Sources.Mock;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DuDi.Cli;

/// <summary>
/// Moduły rejestracji zależności (Composition Root) dla warstw Core, Sources i Agent.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDuDiCore(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRiskRule, VatStatusRiskRule>();
        services.AddSingleton<IRiskRule, BankAccountRiskRule>();
        services.AddSingleton<IRiskRule, KrsStatusRiskRule>();
        services.AddSingleton<IRiskRule, CompanyAgeRiskRule>();
        services.AddSingleton<IRiskRule, BeneficialOwnersRiskRule>();
        services.AddSingleton<IRiskRule, SanctionsRiskRule>();
        services.AddSingleton<IRiskRule, RelationsRiskRule>();

        services.AddSingleton<IRiskScorer, RiskScorer>();
        services.AddSingleton<CounterpartyVerifier>();
        services.AddSingleton<ICounterpartyVerifier>(sp => sp.GetRequiredService<CounterpartyVerifier>());

        return services;
    }

    public static IServiceCollection AddDuDiSources(this IServiceCollection services, DataOptions dataOptions)
    {
        if (dataOptions.Source != DataSourceKind.Mock)
        {
            throw new NotSupportedException(
                "Na tym etapie dostępna jest tylko symulowana baza. Ustaw Data:Source=Mock (prawdziwe API: etapy 3–4).");
        }

        var mockSource = new MockRegistrySource(dataOptions.Mock);

        services.AddSingleton(mockSource);
        services.AddSingleton<IVatWhiteListSource>(sp => sp.GetRequiredService<MockRegistrySource>());
        services.AddSingleton<IKrsSource>(sp => sp.GetRequiredService<MockRegistrySource>());
        services.AddSingleton<ICrbrSource>(sp => sp.GetRequiredService<MockRegistrySource>());
        services.AddSingleton<ISanctionsSource>(sp => sp.GetRequiredService<MockRegistrySource>());
        services.AddSingleton<IPersonRelationsSource>(sp => sp.GetRequiredService<MockRegistrySource>());
        services.AddSingleton<IDemoCompanyCatalog>(sp => sp.GetRequiredService<MockRegistrySource>());

        return services;
    }

    public static IServiceCollection AddDuDiAgent(
        this IServiceCollection services,
        AgentOptions agentOptions,
        bool isTestData,
        bool modelSupportsThinking)
    {
        services.AddSingleton(new ContextBuilder(isTestData));
        services.AddSingleton<RegistryTools>();
        services.AddSingleton<ContextInjector>();
        services.AddSingleton<IChatClient>(_ => ChatClientFactory.Create(agentOptions));

        services.AddSingleton(sp =>
        {
            var useTools = agentOptions.ContextMode == ContextMode.Tools;
            var tools = sp.GetRequiredService<RegistryTools>();
            var chatOptions = ChatClientFactory.CreateChatOptions(
                agentOptions,
                useTools ? tools.AsAITools() : null,
                modelSupportsThinking);
            var contextInjector = useTools ? null : sp.GetRequiredService<ContextInjector>();
            var chatClient = sp.GetRequiredService<IChatClient>();

            return new DuDiAgent(chatClient, chatOptions, SystemPrompt.Load(), contextInjector);
        });

        services.AddSingleton<ChatLoop>();

        return services;
    }
}
