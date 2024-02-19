using TimeSeriesData;

namespace SmartHome.entities;

internal struct AggregatedData
{
    internal readonly int Min;
    internal readonly int Max;
    internal readonly int Avg;
    internal readonly int Cnt;
    internal readonly int Sum;

    internal AggregatedData(IEnumerable<int> data)
    {
        Max = int.MinValue;
        Min = int.MaxValue;
        Sum = 0;
        Cnt = 0;
        foreach (var d in data)
        {
            Sum += d;
            Cnt++;
            if (d > Max)
                Max = d;
            if (d < Min)
                Min = d;
        }
        Avg = Sum / Cnt;
    }
}

internal class SensorData: ILoader<SensorDataItem>
{
    // map sensorId -> map eventTime-> data type map
    internal readonly Dictionary<int, Dictionary<int, Dictionary<string, int>>> Data;
    // map sensorId -> map dataType-> aggregated data
    internal Dictionary<int, Dictionary<string, AggregatedData>> Aggregated;

    public SensorData()
    {
        Data = new Dictionary<int, Dictionary<int, Dictionary<string, int>>>();
        Aggregated = new Dictionary<int, Dictionary<string, AggregatedData>>();
    }
    
    public void Load<T>(string fileName, IDatedSource<T> source, int date)
    {
        var sensorId = int.Parse(Path.GetFileNameWithoutExtension(fileName));
        var rawData = source.Load(fileName, date);
        if (!Data.TryGetValue(sensorId, out var data))
        {
            data = new Dictionary<int, Dictionary<string, int>>();
            Data[sensorId] = data;
        }
        foreach (var d in rawData)
            data[d.EventTime] = d.Data;
    }

    internal void Aggregate()
    {
        Aggregated = Data.Select(v => (v.Key, Aggregate(v.Value))).ToDictionary();
    }

    private static Dictionary<string, AggregatedData> Aggregate(Dictionary<int, Dictionary<string, int>> data)
    {
        return data.Values
            .Aggregate(new Dictionary<string, List<int>>(), Append)
            .Select(kv => (kv.Key, new AggregatedData(kv.Value)))
            .ToDictionary();
    }

    private static Dictionary<string, List<int>> Append(Dictionary<string, List<int>> dict, Dictionary<string, int> value)
    {
        foreach (var kv in value)
        {
            if (!dict.TryGetValue(kv.Key, out var data))
            {
                data = new List<int>();
                dict[kv.Key] = data;
            }
            data.Add(kv.Value);
        }
        return dict;
    }
}

internal class SensorDataItem: IIdentifiable, IBinaryData<SensorDataItem>
{
    public int EventTime { get; set; }
    public Dictionary<string, int> Data { get; set; }
    
    public int GetId()
    {
        return EventTime;
    }

    public static SensorDataItem Create(BinaryReader stream)
    {
        throw new NotImplementedException();
    }

    public void Save(BinaryWriter stream)
    {
        throw new NotImplementedException();
    }
}