using System.Numerics;
using Autumn.Background;
using Autumn.Enums;
using Autumn.FileSystems;
using Autumn.Storage;
using Autumn.Utils;
using Autumn.Wrappers;

namespace Autumn.Rendering.Storage;

internal class ActorSceneObj : IStageSceneObj
{
    public StageObj StageObj { get; }
    public Actor Actor { get; set; }

    public Matrix4x4 Transform { get; set; }
    public AxisAlignedBoundingBox AABB { get; set; }

    public uint PickingId { get; set; }
    public bool Selected { get; set; }
    public bool Hovering { get; set; }
    public bool IsVisible { get; set; } = true;
    public Vector3 DeltaTranslation;
    public Vector3 DeltaScale = Vector3.One;
    public Vector3 DeltaRotation;

    public ActorSceneObj(StageObj stageObj, Actor actorObj, uint pickingId)
    {
        StageObj = stageObj;
        Actor = actorObj;
        PickingId = pickingId;

        AABB = actorObj.AABB;

        UpdateTransform();
    }

    public void UpdateTransform()
    {
        Vector3 scale = Actor.IsEmptyModel ? StageObj.Scale : (StageObj.Scale * DeltaScale) * 0.01f;
        Transform = MathUtils.CreateTransformWithDelta(StageObj.Translation * 0.01f, DeltaTranslation * 0.01f, scale , StageObj.Rotation + DeltaRotation);
    }

    public void UpdateActor(LayeredFSHandler fsHandler, Scene scn, GLTaskScheduler scheduler)
    {
        string actorName = StageObj.Name;

        if (
            StageObj.Properties.TryGetValue("ModelName", out object? modelName)
            && modelName is string modelNameString
            && !string.IsNullOrEmpty(modelNameString)
        )
            actorName = modelNameString;

        DeltaTranslation = Vector3.Zero;
        DeltaScale = Vector3.One;
        DeltaRotation = Vector3.Zero;

        fsHandler.ReadCreatorClassNameTable().TryGetValue(actorName, out string? actorClass);

        Actor = fsHandler.ReadActorNew(actorName, actorClass, scheduler);

        if (actorClass != null && ClassModifiersWrapper.ModifierEntries.ContainsKey(actorClass)) 
        {
            fsHandler.ReadActorExtras(actorName, actorClass, Actor, scheduler);
        
            ClassModifiersWrapper.ModifierEntry? act = null;
            if (ClassModifiersWrapper.ModifierEntries[actorClass].Variants != null && ClassModifiersWrapper.ModifierEntries[actorClass].Variants.ContainsKey(actorName))
            {
                act = ClassModifiersWrapper.ModifierEntries[actorClass].Variants![actorName]!.Value;
            }
            else if (ClassModifiersWrapper.ModifierEntries[actorClass].Default != null)
            {
                act = ClassModifiersWrapper.ModifierEntries[actorClass].Default!.Value;
            }
            if (act is not null)
            {
                if (act.Value.Translation != null) 
                    DeltaTranslation = act.Value.Translation.Value;
                if (act.Value.Scale != null) 
                    DeltaScale = act.Value.Scale.Value;
                if (act.Value.Rotation != null) 
                    DeltaRotation = act.Value.Rotation.Value;
            }
        }

        

        scheduler.EnqueueGLTask( gl => 
            {
                scn.Opaque.Remove(this);
                scn.Translucent.Remove(this);
                scn.Subtractive.Remove(this);
                scn.Additive.Remove(this);
                if (Actor.IsEmptyModel) scn.Opaque.Add(this);
                else
                {
                    if (Actor.CountMeshesLayer(H3DMeshLayer.Opaque) > 0) scn.Opaque.Add(this);
                    if (Actor.CountMeshesLayer(H3DMeshLayer.Translucent) > 0) scn.Translucent.Add(this);
                    if (Actor.CountMeshesLayer(H3DMeshLayer.Subtractive) > 0) { scn.Subtractive.Add(this);}
                    if (Actor.CountMeshesLayer(H3DMeshLayer.Additive) > 0) { scn.Additive.Add(this);}
                }
            }
        );
        UpdateTransform();
    }
}
