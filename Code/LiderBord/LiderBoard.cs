using System;
using Sandbox;

public sealed class LiderBoard : Component
{   
    public static LiderBoard Instance;

    [Property] public int MoveCount = 0; 
    [Property] public float ElapsedTime = 0; 
    [Property] public bool Started = false; 

    protected override void OnStart()
	{
        Instance = this;
    }
    protected override void OnUpdate()
	{
		if (Started == true )
        {
            ElapsedTime += Time.Delta;
        }
	}
    public void AddMoveCount()
    {
        MoveCount ++;
    }
    public void StarTimer()
    {
        Started = true;
    }
    public int GetMoveCount()
    {
        return MoveCount;
    }
    public float GetElapsedTime()
    {
        return ElapsedTime;
    }
}
