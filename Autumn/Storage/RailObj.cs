using Autumn.Enums;

namespace Autumn.Storage;

internal class RailObj : StageObj
{
    public RailPointType PointType { get; set; }

    public int RailNo = 0;

    public bool Closed = false;

    public List<RailPoint> Points { get; init; } = new();

    public static bool operator ==(RailObj r, RailObj rb)
    {
        return r.RailNo == rb.RailNo && r.Closed == rb.Closed && r.Name == rb.Name;
    }

    public static bool operator !=(RailObj r, RailObj rb)
    {
        return r.RailNo != rb.RailNo || r.Closed != rb.Closed || r.Name != rb.Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not RailObj rail)
            return false;

        return RailNo == rail.RailNo && Closed == rail.Closed && Name == rail.Name;
    }
}
