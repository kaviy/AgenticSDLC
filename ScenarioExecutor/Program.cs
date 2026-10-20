using ScenarioExecutor.Scenarios;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           Agentic SDLC Orchestration Platform                   ║");
        Console.WriteLine("║                 URL Shortener Service Demo                       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝\n");

        try
        {
            // Select scenario based on command line argument
            var scenario = args.FirstOrDefault() ?? "all";

            if (scenario == "greenfield" || scenario == "all")
            {
                var greenfieldScenario = new GreenfieldsScenario();
                await greenfieldScenario.ExecuteAsync();
                Console.WriteLine("\n" + new string('=', 70) + "\n");
            }

            if (scenario == "brownfield" || scenario == "all")
            {
                var brownfieldScenario = new BrownfieldScenario();
                await brownfieldScenario.ExecuteAsync();
                Console.WriteLine("\n" + new string('=', 70) + "\n");
            }

            if (scenario == "ambiguous" || scenario == "all")
            {
                var ambiguousScenario = new AmbiguousScenario();
                await ambiguousScenario.ExecuteAsync();
                Console.WriteLine("\n" + new string('=', 70) + "\n");
            }

            Console.WriteLine("✓ All scenarios completed successfully!");
            Console.WriteLine("\nTo inspect workflow executions, check the './workflows' directory.");
            Console.WriteLine("Each execution is saved as a JSON file with full audit trail and metrics.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
