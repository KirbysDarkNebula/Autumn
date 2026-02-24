using System.Numerics;
using Autumn.Background;
using Autumn.Enums;
using Autumn.FileSystems;
using Autumn.Rendering.CtrH3D;
using Autumn.Storage;
using Autumn.Utils;
using Autumn.Wrappers;

namespace Autumn.Rendering.Storage;

internal class ActorSceneObj : IStageSceneObj
{
    public StageObj StageObj { get; }
    public Actor Actor { get; set; }
    public List<Actor> SubActors = new(); // Flagpole top, Arg additions, etc
    public List<Transform> SubActorTransforms = new(); //Transform
    /// <summary>
    /// Number of subactors this actor has by default, without args
    /// </summary>
    public int BaseSubActorCount = 0; 

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
        SubActorTransforms = new();

        fsHandler.ReadCreatorClassNameTable().TryGetValue(actorName, out string? actorClass);

        Actor = fsHandler.ReadActorNew(actorName, actorClass, scheduler);

        if (actorClass != null && ClassModifiersWrapper.ModifierEntries.ContainsKey(actorClass)) 
        {
            fsHandler.ReadActorExtras(actorName, actorClass, this, scheduler);
        
            ClassModifiersWrapper.ModifierEntry? act = ClassModifiersWrapper.GetEntry(actorName, actorClass);
            if (act is not null)
            {
                if (act.Value.Translation != null) 
                    DeltaTranslation = act.Value.Translation.Value;
                if (act.Value.Scale != null) 
                    DeltaScale = act.Value.Scale.Value;
                if (act.Value.Rotation != null) 
                    DeltaRotation = act.Value.Rotation.Value;
                if (act.Value.ExtraModels != null)
                {
                    foreach(Actor ac in SubActors)
                    {
                        SubActorTransforms.Add(new());
                        if (act.Value.ExtraModels[ac.Name] != null)
                        {
                            if (act.Value.ExtraModels[ac.Name]!.Value.Translation != null) 
                                SubActorTransforms[BaseSubActorCount].Translate = act.Value.ExtraModels[ac.Name]!.Value.Translation!.Value;
                            if (act.Value.ExtraModels[ac.Name]!.Value.Scale != null) 
                                SubActorTransforms[BaseSubActorCount].Scale = act.Value.ExtraModels[ac.Name]!.Value.Scale!.Value;
                            if (act.Value.ExtraModels[ac.Name]!.Value.Rotation != null) 
                                SubActorTransforms[BaseSubActorCount].Rotate = act.Value.ExtraModels[ac.Name]!.Value.Rotation!.Value;
                        }
                        BaseSubActorCount += 1;
                    }
                }
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

    public void UpdateActorFromArg(LayeredFSHandler fsHandler, ClassModifiersWrapper.ModifierEntry entry, string arg, GLTaskScheduler scheduler)
    {
        switch (entry.Args![arg].ArgType)
        {
            case ArgType.Tower: // Add actors on top or below, if we add below we move the actor upwards
                string baseModel = entry.Args![arg].RepeatModel ?? Actor.Name;
                Vector3 offset = entry.Args![arg].Offset ?? Vector3.UnitY * 100;
                bool BottomUp = entry.Args![arg].CountTop != null && entry.Args![arg].CountTop!.Value;
                int start = SubActors.Count - BaseSubActorCount;
                int end = int.Clamp((int)StageObj.Properties[arg]!, BottomUp ? 1 : 0, 10) - (BottomUp ? 1 : 0); // Default is 3 / -1 is 3, max is 10, min is 0 but let's not


                if (start > end)
                {
                    for (int j = start-1; j >= end; j--)
                    {
                        SubActors.RemoveAt(j);
                        SubActorTransforms.RemoveAt(j);
                        if (!BottomUp)
                            DeltaTranslation = offset * (j);
                    }
                }
                else
                {
                    for (int j = start; j < end; j++)
                    {
                        Actor? SubAct = fsHandler.ReadActorExtrasArg(baseModel, scheduler);
                        if (SubAct is null)
                            continue;
                        SubActors.Add(SubAct);
                        SubActorTransforms.Add(new() { Translate = (BottomUp ? 1 : -1)* offset * (j+1)});
                        if (!BottomUp)
                            DeltaTranslation = offset * (j+1);
                    }
                }
                AABB = Actor.AABB * (end > 0 ? end : 1);
                UpdateTransform();

            break;

            default:
            
            break;
        } 
    }
}
