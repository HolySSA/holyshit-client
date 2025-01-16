

using UnityEngine;

public static class ProtoExtension
{
  /// <summary>
  /// 캐릭터 위치 데이터를 벡터로 변환
  /// </summary>
  public static Vector3 PositionToVector(this CharacterPositionData positionData)
  {
    return new Vector3(
      (float)positionData.X,
      (float)positionData.Y,
      0f
    );
  }
}