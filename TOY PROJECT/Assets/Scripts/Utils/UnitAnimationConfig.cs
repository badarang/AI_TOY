using UnityEngine;

public static class UnitAnimationConfig
{
    public const float BASE_ATTACK_APPROACH_DURATION = 0.3f;
    public const float BASE_ATTACK_HIT_DURATION = 0.5f;
    public const float BASE_ATTACK_RETURN_DURATION = 0.25f;
    public const float BASE_MOVE_DURATION_PER_TILE = 0.2f;
    public const float BASE_HIT_REACTION_DURATION = 0.3f;
    public const float BASE_FLASH_DURATION = 0.3f;
    public static readonly Color FLASH_COLOR = Color.white;

    
    public const float ATTACK_APPROACH_DISTANCE_RATIO = 0.4f;
    
    public static float GetAttackApproachDuration(float speedMultiplier)
    {
        return BASE_ATTACK_APPROACH_DURATION * speedMultiplier;
    }
    
    public static float GetAttackHitDuration(float speedMultiplier)
    {
        return BASE_ATTACK_HIT_DURATION * speedMultiplier;
    }
    
    public static float GetAttackReturnDuration(float speedMultiplier)
    {
        return BASE_ATTACK_RETURN_DURATION * speedMultiplier;
    }
    
    public static float GetMoveDurationPerTile(float speedMultiplier)
    {
        return BASE_MOVE_DURATION_PER_TILE * speedMultiplier;
    }
    
    public static float GetHitReactionDuration(float speedMultiplier)
    {
        return BASE_HIT_REACTION_DURATION * speedMultiplier;
    }
    
    public static float GetTotalAttackDuration(float speedMultiplier)
    {
        return GetAttackApproachDuration(speedMultiplier) + 
               GetAttackHitDuration(speedMultiplier) + 
               GetAttackReturnDuration(speedMultiplier);
    }


public static float GetFlashDuration(float speedMultiplier)
    {
        return BASE_FLASH_DURATION * speedMultiplier;
    }
}