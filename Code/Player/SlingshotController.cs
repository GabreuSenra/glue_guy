using Sandbox;
using System;

public sealed class SlingshotController : Component, Component.ICollisionListener
{
    [Property, Group("Referências")] public Rigidbody Body { get; set; }
    [Property, Group("Referências")] public CameraComponent MainCamera { get; set; }
    [Property, Group("Referências")] public SpriteRenderer PlayerSprite { get; set; }

    [Property, Group("Elástico")] public float MaxPullDistance { get; set; } = 300f;
    [Property, Group("Elástico")] public float LaunchForceMultiplier { get; set; } = 5f;
    [Property, Group("Aderência")] public float StickTime { get; set; } = 1.5f;
    [Property, Group("Aderência")] public float SlideSpeed { get; set; } = 50f;
    
    [Property, Group("Detecção")] public float GroundCheckDistance { get; set; } = 25f; 
     [Property, Group("Detecção")] public Vector2 GroundCheckOffsets { get; set; } = 25f; 
    [Property, Group("Detecção")] public float WallCheckDistance { get; set; } = 30f; 

    public enum PlayerState { Caindo, Preso, Escorregando, inGround }
    [Property] public PlayerState CurrentState { get; private set; } = PlayerState.Caindo;

    public float CurrentPullPercentage { get; private set; }
    public float LastPullPercentage { get; private set; }
    public bool IsDragging { get; private set; }
    public Vector2 DragStartScreenPos { get; private set; }

    private TimeSince _timeSinceAttached;
    private TimeSince _timeSinceLaunch; 
    private Vector3 _surfaceNormal;

    private Vector2 _baseSpriteSize;

    protected override void OnStart() 
    {
        if(IsProxy) return;

        if ( Body == null ) Body = Components.Get<Rigidbody>();
        if ( PlayerSprite != null ) _baseSpriteSize = PlayerSprite.Size;
    }

    protected override void OnUpdate() 
    {
        if(IsProxy) return;

        Mouse.Visible = true;
        HandleInput();
        UpdateUIValues();
        ApplyJuice();
    }

    protected override void OnFixedUpdate()
    {
        if(IsProxy) return;

        float PostivePos = GroundCheckOffsets.x;
        float NegativePos = GroundCheckOffsets.y;

        CheckGround(PostivePos); 
        CheckGround(0); 
        CheckGround(NegativePos); 
        CheckWall(); 
        HandleStates();
    }

    private void HandleInput()
    {
        if ( Input.Pressed( "attack1" ) && CurrentState != PlayerState.Caindo )
        {
            if ( MainCamera != null )
            {
                Vector2 charScreenPos = MainCamera.PointToScreenPixels( Transform.Position );
                float distanceToMouse = (charScreenPos - Mouse.Position).Length;
                float clickToleranceRadius = 100f; 

                if ( distanceToMouse <= clickToleranceRadius )
                {
                    CurrentPullPercentage = 0f;
                    IsDragging = true;
                    DragStartScreenPos = Mouse.Position;
                }
            }
        }

        if ( Input.Released( "attack1" ) && IsDragging )
        {
            IsDragging = false;
            Launch();
        }
    }

    private void UpdateUIValues()
    {
        if ( IsDragging )
        {
            Vector2 dragVector = DragStartScreenPos - Mouse.Position;
            CurrentPullPercentage = Math.Clamp(dragVector.Length / MaxPullDistance, 0f, 1f);
        }
    }

    private void Launch()
    {
        LastPullPercentage = CurrentPullPercentage;
        CurrentPullPercentage = 0f; 
        _timeSinceLaunch = 0; 

        Vector2 dragVector = DragStartScreenPos - Mouse.Position;
        if ( dragVector.Length > MaxPullDistance ) dragVector = dragVector.Normal * MaxPullDistance;

        Vector3 launchDirection = new Vector3( dragVector.x, 0, -dragVector.y ).Normal;
        float appliedForce = dragVector.Length * LaunchForceMultiplier;

        CurrentState = PlayerState.Caindo;
        Body.Velocity = launchDirection * appliedForce;
    }

    private void CheckGround(float offset)
    {
        Vector3 position = new Vector3(Transform.Position.x + offset, Transform.Position.y,Transform.Position.z);

        var tr = Scene.Trace.Ray( position, position + Vector3.Down * GroundCheckDistance )
            .IgnoreGameObjectHierarchy( GameObject ) 
            .WithoutTags( "player" )
            .Run();

        if ( tr.Hit && tr.Normal.z >= 0.7f )
        {
            if ( CurrentState != PlayerState.inGround && _timeSinceLaunch > 0.1f )
            {
                CurrentState = PlayerState.inGround;
            }
        }
        else if ( CurrentState == PlayerState.inGround )
        {
            CurrentState = PlayerState.Caindo;
        }

        //DebugOverlay.Trace( tr, 5f ); 
    }

    private void CheckWall()
    {
        // Só checamos a parede se estivermos nela
        if ( CurrentState == PlayerState.Preso || CurrentState == PlayerState.Escorregando )
        {
            // A direção da parede é sempre o oposto da Normal da superfície
            Vector3 directionToWall = -_surfaceNormal;

            // Dispara um raio curto do centro do boneco para dentro da parede
            var tr = Scene.Trace.Ray( Transform.Position, Transform.Position + directionToWall * WallCheckDistance )
                .IgnoreGameObjectHierarchy( GameObject )
                .WithoutTags( "player" )
                .Run();

            // Se o raio não acertou nada (a parede acabou) OU acertou um chão plano (quina invertida)
            if ( !tr.Hit || tr.Normal.z >= 0.7f )
            {
                CurrentState = PlayerState.Caindo;
            }
            else
            {
                // Bônus: atualiza a normal continuamente. Se a parede for curva, ele acompanha!
                _surfaceNormal = tr.Normal;
            }
        }
    }

    private void HandleStates()
    {
        if ( CurrentState == PlayerState.Preso )
        {
            Body.Velocity = Vector3.Zero;
            if ( _timeSinceAttached > StickTime ) CurrentState = PlayerState.Escorregando;
        }
        else if ( CurrentState == PlayerState.Escorregando )
        {
            Vector3 down = Vector3.Down;
            Vector3 slideDir = down - (down.Dot(_surfaceNormal) * _surfaceNormal);
            Body.Velocity = slideDir.Normal * SlideSpeed;

            if ( _timeSinceAttached > (StickTime + 2.0f) ) CurrentState = PlayerState.Caindo;
        }
    }

    public void OnCollisionStart( Collision collision )
    {
        TryStickToWall( collision.Contact.Normal );
    }

    // Mantemos apenas para redundância, mas o CheckWall já faz o trabalho principal agora
    public void OnCollisionUpdate( Collision collision ) 
    {
        TryStickToWall( collision.Contact.Normal );
    }
    
    // Deixamos vazio. Sem depender da engine avisar que parou!
    public void OnCollisionStop( Collision collision ) { }

    private void TryStickToWall( Vector3 normal )
    {
        if ( _timeSinceLaunch <= 0.1f ) return; 
        if ( normal.z >= 0.7f ) return;

        if ( CurrentState == PlayerState.Caindo && normal.z > -0.5f ) 
        {
            CurrentState = PlayerState.Preso;
            _timeSinceAttached = 0;
            _surfaceNormal = normal;
            Body.Velocity = Vector3.Zero;
        }
    }


    private void ApplyJuice()
    {
        if ( PlayerSprite == null ) return;

        // Começamos assumindo o tamanho normal
        Vector2 targetSize = _baseSpriteSize;

        if ( IsDragging )
        {
            // Achata levemente enquanto puxa
            float squashAmount = 1f - (CurrentPullPercentage * 0.2f);
            float stretchAmount = 1f + (CurrentPullPercentage * 0.1f);
            targetSize = new Vector2( _baseSpriteSize.x * stretchAmount, _baseSpriteSize.y * squashAmount );
        }
        else if ( CurrentState == PlayerState.Caindo && Body.Velocity.Length > 10f )
        {
            // Estica durante o voo de acordo com a velocidade
            float speedStretch = Math.Clamp(Body.Velocity.Length / 500f, 0f, 0.4f);
            targetSize = new Vector2( _baseSpriteSize.x * (1f - speedStretch * 0.5f), _baseSpriteSize.y * (1f + speedStretch) );
        }
        else if ( CurrentState == PlayerState.Preso || CurrentState == PlayerState.Escorregando )
        {
            // Amassa contra a parede como uma geleca! (Largo no X, achatado no Y)
            targetSize = new Vector2( _baseSpriteSize.x * 1.3f, _baseSpriteSize.y * 0.7f );
        }

        // Interpola suavemente o tamanho atual para o tamanho desejado
        //PlayerSprite.Size = Vector2.Lerp( PlayerSprite.Size, targetSize, Time.Delta * 15f );

        SyncJuice(targetSize);
    }

    [Rpc.Broadcast]
    private void SyncJuice(Vector2 targetSize)
    {
        PlayerSprite.Size = Vector2.Lerp( PlayerSprite.Size, targetSize, Time.Delta * 15f );
    }
}