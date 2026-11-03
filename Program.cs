using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using OllamaSharp;

// ===================== CONFIG =====================
// Use "llama3.2" (lighter) or "qwen2.5:3b" (better at tool calling, needs ~8 GB RAM)
const string ModelName = "qwen2.5:3b";
const string OllamaUrl = "http://localhost:11434";

// ===================== 1. AI CLIENT =====================
// Base client talks to Ollama; UseFunctionInvocation() lets the model call our C# tools automatically
IChatClient baseClient = new OllamaApiClient(new Uri(OllamaUrl), ModelName);
IChatClient chat = baseClient.AsBuilder()
    .UseFunctionInvocation()
    .Build();

// ===================== 2. TOOLS =====================
// Register C# methods as tools the model is allowed to call
var options = new ChatOptions
{
    Tools =
    [
        AIFunctionFactory.Create(GetApplicationStatus),
        AIFunctionFactory.Create(CalculateEmi)
    ]
};

// ===================== 3. MEMORY (conversation history) =====================
List<ChatMessage> history =
[
    new(ChatRole.System, """
        You are a helpful banking assistant.
        Rules:
        - Use GetApplicationStatus ONLY when the user gives an application ID that starts with 'APP' (e.g. APP101).
        - Use CalculateEmi ONLY when the user asks to calculate an EMI or loan payment.
        - For all other questions (greetings, names, general questions), answer directly WITHOUT using any tool.
        - Never guess numbers. Always use the tool result for status and EMI values.
        """)
];

// ===================== 4. CHAT LOOP =====================
Console.WriteLine($"Banking Assistant using '{ModelName}' (type 'exit' to quit)");

while (true)
{
    Console.Write("\nYou: ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    history.Add(new ChatMessage(ChatRole.User, input));

    try
    {
        var response = await chat.GetResponseAsync(history, options);
        Console.WriteLine($"AI: {response.Text}");

        // Save the AI reply (and any tool calls/results) so the model remembers them
        history.AddMessages(response);
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("AI: Cannot reach Ollama. Make sure the Ollama app is running.");
        history.RemoveAt(history.Count - 1); // drop the unanswered user message
    }
}

Console.WriteLine("Goodbye!");

// ===================== TOOL METHODS =====================

[Description("Gets the status of a credit card application. Only use when the user provides an application ID starting with 'APP'. Do NOT use for names or general questions.")]
static string GetApplicationStatus(
    [Description("Application ID, for example APP101")] string applicationId)
{
    Console.WriteLine($"   [Tool called: GetApplicationStatus({applicationId})]");

    // Input validation: never trust what the model sends
    if (!Regex.IsMatch(applicationId.Trim(), @"^APP\d+$", RegexOptions.IgnoreCase))
        return "Invalid application ID. A valid ID looks like APP101. Ask the user for a correct ID.";

    // Fake database (later this becomes a real DB / API call)
    var fakeDb = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["APP101"] = "Approved - credit limit ₹1,50,000",
        ["APP102"] = "Referred - pending underwriter review",
        ["APP103"] = "Declined - low credit score"
    };

    return fakeDb.TryGetValue(applicationId.Trim(), out var status)
        ? status
        : "Application not found";
}

[Description("Calculates the monthly EMI for a loan. Only use when the user asks for an EMI or loan payment calculation.")]
static string CalculateEmi(
    [Description("Loan amount in rupees, e.g. 500000")] double principal,
    [Description("Annual interest rate in percent, e.g. 10.5")] double annualRate,
    [Description("Loan tenure in months, e.g. 36")] int months)
{
    Console.WriteLine($"   [Tool called: CalculateEmi({principal}, {annualRate}, {months})]");

    // Input validation
    if (principal <= 0 || annualRate <= 0 || months <= 0)
        return "Invalid input. Principal, interest rate and months must all be greater than zero.";

    double r = annualRate / 12 / 100;               // monthly interest rate
    double factor = Math.Pow(1 + r, months);
    double emi = principal * r * factor / (factor - 1);

    return $"Monthly EMI: ₹{emi:N0}, Total payable: ₹{emi * months:N0}, Total interest: ₹{emi * months - principal:N0}";
}