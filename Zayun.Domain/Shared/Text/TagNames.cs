using System.Text;

namespace Zayun.Domain.Shared.Text;

/// <summary>
/// How tag names (project tags, experience and skill-group skills) are cleaned and compared, so the same thing is
/// never stored twice under slightly different names.
/// </summary>
/// <remarks>
/// <para><b>Key</b>: the name in UPPERCASE without spaces, dots, dashes or underscores ("ASP.NET Core" → "ASPNETCORE",
/// "rest-api" → "RESTAPI"). Two names with the same key are the same tag. "#", "+", "/" and "&amp;" are kept, so "C#"
/// and "C" stay different.</para>
/// <para><b>Built-in synonyms</b>: common names that mean the same thing ("RESTful API", "REST APIs" → "REST API";
/// "MS SQL" → "SQL Server") and the usual spelling of well-known names ("typescript" → "TypeScript"). New tags are
/// checked against these first, for free; anything else can be checked by the AI tag check.</para>
/// </remarks>
public static class TagNames
{
    /// <summary>Longest tag name.</summary>
    public const int MaxLength = 60;

    /// <summary>Longest key (the key is never longer than the name).</summary>
    public const int KeyMaxLength = MaxLength;

    /// <summary>Trimmed, with runs of spaces made single ("  Machine   Learning " → "Machine Learning").</summary>
    /// <param name="name">What was typed.</param>
    /// <returns>The cleaned name (may be empty).</returns>
    public static string Clean(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var builder = new StringBuilder(name.Length);
        var lastWasSpace = false;
        foreach (var c in name.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace) builder.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                builder.Append(c);
                lastWasSpace = false;
            }
        }
        return builder.ToString();
    }

    /// <summary>The uppercase comparison key ("Entity Framework Core" → "ENTITYFRAMEWORKCORE").</summary>
    /// <param name="name">A tag name.</param>
    /// <returns>The key (empty for an empty name).</returns>
    public static string Key(string? name)
    {
        var builder = new StringBuilder();
        foreach (var c in Clean(name))
        {
            if (char.IsWhiteSpace(c) || c is '.' or '-' or '_' or '–' or '—') continue;
            builder.Append(char.ToUpperInvariant(c));
        }
        return builder.ToString();
    }

    /// <summary>The built-in name for a synonym or a well-known name, e.g. "restful apis" → "REST API".</summary>
    /// <param name="name">A tag name.</param>
    /// <returns>The built-in name, or null when it isn't a known one.</returns>
    public static string? BuiltIn(string? name) =>
        BuiltInNames.TryGetValue(Key(name), out var canonical) ? canonical : null;

    /// <summary>
    /// The same key in the singular or plural ("RESTAPIS" ↔ "RESTAPI", "TRANSFORMER" ↔ "TRANSFORMERS"), used to catch
    /// "REST APIs" when "REST API" already exists. Short keys are left alone so "CSS" and "CS" don't meet.
    /// </summary>
    /// <param name="key">A key.</param>
    /// <returns>The other form, or null.</returns>
    public static string? OtherNumber(string key)
    {
        if (key.Length < 4) return null;
        return key.EndsWith('S') ? key[..^1] : key + "S";
    }

    /// <summary>Synonyms and well-known spellings, by key. The value is the name used.</summary>
    private static readonly Dictionary<string, string> BuiltInNames = Build(
        // APIs and architecture
        ("REST API", ["REST API", "REST APIs", "RESTful API", "RESTful APIs", "RESTful", "RESTful Services",
                      "REST Services", "REST"]),
        ("GraphQL", ["GraphQL", "Graph QL"]),
        ("gRPC", ["gRPC"]),
        ("CQRS", ["CQRS"]),
        ("MediatR", ["MediatR"]),
        ("Clean Architecture", ["Clean Architecture"]),
        ("Microservices", ["Microservices", "Microservice", "Micro Services", "Microservice Architecture"]),
        ("CI/CD", ["CI/CD", "CICD", "CI CD", "CI-CD", "Continuous Integration", "Continuous Delivery",
                   "Continuous Deployment"]),

        // .NET
        (".NET", [".NET", "dotnet", "dot net"]),
        ("ASP.NET Core", ["ASP.NET Core", "ASP NET Core", "aspnetcore", "ASP.NET Core Web API", "ASP.NET Web API Core"]),
        ("ASP.NET", ["ASP.NET", "ASP NET"]),
        (".NET MAUI", [".NET MAUI", "MAUI", "dotnet MAUI"]),
        ("C#", ["C#", "CSharp", "C Sharp"]),
        ("Entity Framework Core", ["Entity Framework Core", "EF Core", "EFCore"]),
        ("WPF", ["WPF", "Windows Presentation Foundation"]),
        ("Windows Forms", ["Windows Forms", "WinForms", "Win Forms"]),
        ("Blazor", ["Blazor"]),

        // Web
        ("JavaScript", ["JavaScript", "JS", "Java Script", "ECMAScript"]),
        ("TypeScript", ["TypeScript", "TS", "Type Script"]),
        ("React", ["React", "ReactJS", "React.js", "React JS"]),
        ("React Native", ["React Native"]),
        ("Next.js", ["Next.js", "NextJS"]),
        ("Node.js", ["Node.js", "NodeJS"]),
        ("Angular", ["Angular", "AngularJS"]),
        ("Vue.js", ["Vue.js", "Vue", "VueJS"]),
        ("HTML", ["HTML", "HTML5"]),
        ("CSS", ["CSS", "CSS3"]),
        ("Tailwind CSS", ["Tailwind CSS", "Tailwind", "TailwindCSS"]),
        ("Vite", ["Vite", "ViteJS"]),

        // Data
        ("SQL Server", ["SQL Server", "MS SQL", "MSSQL", "MS SQL Server", "Microsoft SQL Server", "SQLServer"]),
        ("PostgreSQL", ["PostgreSQL", "Postgres", "Postgre SQL", "PGSQL"]),
        ("MySQL", ["MySQL", "My SQL"]),
        ("SQLite", ["SQLite"]),
        ("MongoDB", ["MongoDB", "Mongo", "Mongo DB"]),
        ("Redis", ["Redis"]),
        ("SQL", ["SQL"]),

        // Python and AI
        ("Python", ["Python", "Python3", "Python 3"]),
        ("Django", ["Django"]),
        ("FastAPI", ["FastAPI", "Fast API"]),
        ("PyTorch", ["PyTorch"]),
        ("TensorFlow", ["TensorFlow", "Tensor Flow"]),
        ("scikit-learn", ["scikit-learn", "sklearn", "scikit learn"]),
        ("NumPy", ["NumPy"]),
        ("pandas", ["pandas"]),
        ("Artificial Intelligence", ["Artificial Intelligence", "AI"]),
        ("Machine Learning", ["Machine Learning", "ML"]),
        ("Deep Learning", ["Deep Learning", "DL"]),
        ("Natural Language Processing", ["Natural Language Processing", "NLP"]),
        ("Large Language Models", ["Large Language Models", "Large Language Model", "LLM", "LLMs"]),
        ("Computer Vision", ["Computer Vision"]),
        ("Vision Transformers", ["Vision Transformers", "Vision Transformer", "ViT", "ViTs"]),
        ("Transformers", ["Transformers", "Transformer", "Transformer Models"]),
        ("Explainable AI", ["Explainable AI", "XAI", "Explainable Artificial Intelligence"]),
        ("Multimodal AI", ["Multimodal AI", "Multi-modal AI", "Multimodal Artificial Intelligence"]),
        ("MATLAB", ["MATLAB", "Matlab"]),

        // Cloud and tools
        ("Docker", ["Docker"]),
        ("Kubernetes", ["Kubernetes", "K8s"]),
        ("Microsoft Azure", ["Microsoft Azure", "Azure"]),
        ("Amazon Web Services", ["Amazon Web Services", "AWS"]),
        ("Google Cloud", ["Google Cloud", "GCP", "Google Cloud Platform"]),
        ("Git", ["Git"]),
        ("GitHub", ["GitHub", "Git Hub"]),
        ("GitHub Actions", ["GitHub Actions", "GH Actions"]),
        ("PyCharm", ["PyCharm"]),
        ("Visual Studio", ["Visual Studio"]),
        ("IIS", ["IIS", "Internet Information Services"]));

    private static Dictionary<string, string> Build(params (string Name, string[] Synonyms)[] groups)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, synonyms) in groups)
        {
            map[Key(name)] = name;
            foreach (var synonym in synonyms) map[Key(synonym)] = name;
        }
        return map;
    }
}
