/// <summary>
/// 공격 가능한 오브젝트(유닛, 포탈 등)를 위한 인터페이스입니다.
/// </summary>
public interface IAttackable
{
    /// <summary>
    /// 개체에 데미지를 적용합니다.
    /// </summary>
    /// <param name="amount">적용할 데미지 양</param>
    void TakeDamage(int amount);
}
