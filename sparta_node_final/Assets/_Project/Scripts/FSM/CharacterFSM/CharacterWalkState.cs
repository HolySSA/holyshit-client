using Ironcow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterWalkState : CharacterState
{
    private const float SYNC_INTERVAL = 0.5f;  // 동기화 간격 (100ms) - 10fps
    private float syncTimer = 0f;
    private Vector2 lastSyncPosition;  // 마지막으로 동기화된 위치

    /// <summary>
    /// 걷기 상태 진입 시 호출되는 메서드
    /// </summary>
    public override void OnStateEnter()
    {
        if (anim != null)
        {
            anim.ChangeAnimation("walk");
        }

        lastSyncPosition = character.transform.position;
    }

    /// <summary>
    /// 걷기 상태 종료 시 호출되는 메서드
    /// </summary>
    public override void OnStateExit()
    {
        anim.ChangeAnimation("idle");
    }

    /// <summary>
    /// 걷기 상태 업데이트 시 매 프레임 호출되는 메서드
    /// </summary>
    public override void OnStateUpdate()
    {
        // 걷기 애니메이션으로 변경
        if (anim != null && !anim.IsAnim("walk"))
        {
            anim.ChangeAnimation("walk");
        }

        // 방향과 속도에 따라 이동
        rigidbody.linearVelocity = character.dir * character.Speed;

        // 위치 동기화
        if (SocketManager.instance.isConnected && character.dir != Vector2.zero)
        {
            syncTimer += Time.deltaTime;

            // 일정 시간마다 또는 일정 거리 이상 이동했을 때 동기화
            Vector2 currentPos = character.transform.position;
            float movedDistance = Vector2.Distance(lastSyncPosition, currentPos);

            // 서버에 위치 정보 전송
            if (syncTimer >= SYNC_INTERVAL || movedDistance > 0.5f)
            {
                GamePacket packet = new GamePacket();
                packet.PositionUpdateRequest = new C2SPositionUpdateRequest()
                {
                    X = currentPos.x,
                    Y = currentPos.y,
                };

                // 전송
                SocketManager.instance.Send(packet);
                // 초기화
                syncTimer = 0f;
                lastSyncPosition = currentPos;
            }
        }
    }
}
