using Sandbox;

public sealed class CameraFollow : Component
{

    [Property] public GameObject PlayerObject;

    protected override void OnUpdate()
    {

        if(PlayerObject == null){
            var sc = Scene.GetAllComponents<SlingshotController>()
                .FirstOrDefault( x => !x.IsProxy );

            PlayerObject = sc.GameObject;
        }

        if(PlayerObject != null)
        {
            GameObject.WorldPosition = new Vector3(PlayerObject.WorldPosition.x, GameObject.WorldPosition.y,  PlayerObject.WorldPosition.z);
        }
    }

}