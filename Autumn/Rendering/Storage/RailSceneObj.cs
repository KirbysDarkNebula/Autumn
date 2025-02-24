using System.Numerics;
using Autumn.Rendering.Rail;
using Autumn.Storage;

namespace Autumn.Rendering.Storage;

internal class RailSceneObj : ISceneObj
{
    public RailObj RailObj { get; }
    public RailModel RailModel { get; }

    public Matrix4x4 Transform { get; set; }
    public AxisAlignedBoundingBox AABB { get; set; }

    public uint PickingId { get; set; }
    public bool Selected { get; set; }
    public bool IsVisible { get; set; } = true;

    public List<uint> PointsPickingIds { get; init; }
    public List<bool> PointsSelected { get; init; }

    StageObj ISceneObj.StageObj => RailObj;

    public RailSceneObj(RailObj rail, RailModel railModel, ref uint pickingId)
    {
        RailObj = rail;
        RailModel = railModel;
        PickingId = pickingId++;

        AABB = new(); // TO-DO

        int pointCount = rail.Points.Count;
        PointsPickingIds = new(pointCount);
        PointsSelected = [.. new bool[pointCount]];

        for (int i = 0; i < pointCount; i++)
            PointsPickingIds.Add(pickingId++);

        UpdateTransform();
    }

    public void UpdateTransform() => Transform = Matrix4x4.Identity;
}
