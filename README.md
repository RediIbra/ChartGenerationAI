# ChartGenerationAI

ChartGenerationAI is a project that allows you to **generate Highcharts charts from a single prompt** using OpenAI's API.

You can create line charts, bar charts, pie charts, bubble charts, and more by simply providing a natural language description of the chart you want.

---

## Setup

1. **Clone the repository**:

```bash
git clone https://github.com/RediIbra/ChartGenerationAI.git
cd ChartGenerationAI
```

2. **Create an OpenAI API Key**:

Follow these steps:

1. Go to [OpenAI API Keys](https://platform.openai.com/account/api-keys).

2. Log in with your OpenAI account.

3. Click on **“Create new secret key”**.

4. Copy the generated key. **Keep it safe**, you won’t see it again.

5. **Store your API key locally using User Secrets**:

```bash
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-api-key-here"
```

> This ensures your key is **never pushed to GitHub**.

4. **Run the project**:

```bash
dotnet run
```

---

## Usage

* Open the application in your browser.
* Navigate to a chart page (Line, Bar, Pie, etc.).
* Enter your chart prompt in natural language.
* The app will generate the Highcharts JSON configuration and render the chart dynamically.

---

## Notes

* Make sure `appsettings.Development.json` is **ignored** in `.gitignore` to avoid exposing your API key.
* The project uses **ASP.NET Core MVC** with **Highcharts** for chart rendering.
<img width="1918" height="1021" alt="Screenshot 2025-08-25 104540" src="https://github.com/user-attachments/assets/8c30b9c6-c606-4a6b-85c8-020f4fcaeb05" />
