using System;
using Sandbox;
public sealed class LiderBoard : Component
{
    [Property] public int movecont = 0; 
    [Property] public float elapsedtime = 0; 
    [Property] public bool started = false; 

    public void contador()
    {
        movecont ++;
        
    }

    public void startimer()
    {
        started = true;

    }

	protected override void OnUpdate()
	{
		if (started == true )
        {
            elapsedtime += Time.Delta;

        }

	}


}


//TESTANDO
