using System.Text.Json;

namespace highcharts_demo.Models
{
    public class UpdateChartRequest
    {
        public JsonElement CurrentConfig { get; set; }
        public string Instruction { get; set; }
    }
}
