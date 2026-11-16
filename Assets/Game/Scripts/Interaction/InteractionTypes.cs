namespace Game.Interaction
{
    public enum InteractionTypes : int
    {
        None = 0,
        Load,
        Unload,
        // 방해물 상호작용(Story 010). 직렬화 값이 밀리지 않도록 끝에 추가한다
        Obstacle,
    }
}