using Sandbox;
using System;

public sealed class EyeController : Component
{
    [Property, Group("Referências")] public CameraComponent MainCamera { get; set; }
    
    [Property, Group("Configurações")] public float MouseReachDistance { get; set; } = 200f;
    [Property, Group("Configurações")] public float EyeFollowSpeed { get; set; } = 20f;

    // Coloque os valores sempre positivos aqui. O código cuida da inversão matemática!
    [Property, Group("Limites (Lados)")] public float LimiteDireita { get; set; } = 5f;
    [Property, Group("Limites (Lados)")] public float LimiteEsquerda { get; set; } = 4f; 

    [Property, Group("Limites (Cima/Baixo)")] public float LimiteCima { get; set; } = 5f;
    [Property, Group("Limites (Cima/Baixo)")] public float LimiteBaixo { get; set; } = 6f; 

    public enum EixoLocal { X, Y, Z }

    [Property, Group("Mapeamento de Eixos")] 
    public EixoLocal EixoParaOsLados { get; set; } = EixoLocal.Y;
    
    [Property, Group("Mapeamento de Eixos")] 
    public EixoLocal EixoParaCimaBaixo { get; set; } = EixoLocal.Z;

    // Guarda a posição original exata para o olho não sair voando pelo mapa
    private Vector3 _posicaoInicial;

    protected override void OnStart()
    {
        if(IsProxy) return;

        // Salva onde você posicionou o olho no Editor da S&box como o "Centro" (0,0) dele
        _posicaoInicial = Transform.LocalPosition;
    }

    protected override void OnUpdate()
    {   
        if(IsProxy) return;

        if ( MainCamera == null ) return;

        Vector2 eyeScreenPos = MainCamera.PointToScreenPixels( Transform.Position );
        Vector2 mouseDir = Mouse.Position - eyeScreenPos;
        float distance = mouseDir.Length;
        
        float normalizedDist = Math.Clamp( distance / MouseReachDistance, 0f, 1f );
        Vector2 circlePos = mouseDir.Normal * normalizedDist;

        // 1. Calcula o Deslocamento Horizontal
        float offsetHorizontal = 0f;
        if (circlePos.x > 0)
            offsetHorizontal = circlePos.x * LimiteDireita;
        else
            offsetHorizontal = circlePos.x * LimiteEsquerda; // circlePos.x já é negativo, então vira -4 automaticamente

        // 2. Calcula o Deslocamento Vertical (S&box: Mouse Y desce, Z sobe)
        float offsetVertical = 0f;
        if (circlePos.y > 0) 
            offsetVertical = -circlePos.y * LimiteBaixo; // Mouse desceu, limite -6
        else 
            offsetVertical = -circlePos.y * LimiteCima; // Mouse subiu, limite 5

        // 3. Distribui os movimentos para os eixos que você escolheu no Inspector
        Vector3 targetOffset = Vector3.Zero;

        // Eixo dos lados
        if (EixoParaOsLados == EixoLocal.X) targetOffset.x = offsetHorizontal;
        else if (EixoParaOsLados == EixoLocal.Y) targetOffset.y = offsetHorizontal;
        else if (EixoParaOsLados == EixoLocal.Z) targetOffset.z = offsetHorizontal;

        // Eixo de cima/baixo
        if (EixoParaCimaBaixo == EixoLocal.X) targetOffset.x = offsetVertical;
        else if (EixoParaCimaBaixo == EixoLocal.Y) targetOffset.y = offsetVertical;
        else if (EixoParaCimaBaixo == EixoLocal.Z) targetOffset.z = offsetVertical;

        // 4. Soma o deslocamento à posição original (mantém a profundidade intacta)
        Vector3 targetPos = _posicaoInicial + targetOffset;
        
        ApplyMovement(targetPos);
        //Transform.LocalPosition = Vector3.Lerp( Transform.LocalPosition, targetPos, Time.Delta * EyeFollowSpeed );
    }

    [Rpc.Broadcast]
    private void ApplyMovement(Vector3 targetPos)
    {
        Transform.LocalPosition = Vector3.Lerp( Transform.LocalPosition, targetPos, Time.Delta * EyeFollowSpeed );
    }
}