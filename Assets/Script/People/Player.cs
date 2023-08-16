using Chat;
using Photon.Chat;
using UnityEngine;

public class Player : MonoBehaviour
{

    [SerializeField] Rigidbody rigitBody;
    [SerializeField] Animator anim;
    [SerializeField] Transform cameraTrans;
    [SerializeField] GroundChek groundChek;

    [SerializeField] float normalSpeed = 3f; // 通常時の移動速度
    [SerializeField] float sprintSpeed = 5f; // ダッシュ時の移動速度
    [SerializeField] float jump = 8f;        // ジャンプ力

    Vector3 moveDirection = Vector3.zero;

    [SerializeField] private GameObject chatStartObj = null;
    [SerializeField] private float distance = 5f;
    private Friend friend = null;

    void Update()
    {
        // 話しているときは動かない
        if (ChatController.Instance.ChatClient != null && ChatState.Disconnected != ChatController.Instance.ChatClient.State)
            return;

        // 移動速度を取得
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : normalSpeed;

        // カメラの向きを基準にした正面方向のベクトル
        Vector3 cameraForward = Vector3.Scale(cameraTrans.forward, new Vector3(1, 0, 1)).normalized;

        // 前後左右の入力（WASDキー）から、移動のためのベクトルを計算
        // Input.GetAxis("Vertical") は前後（WSキー）の入力値
        // Input.GetAxis("Horizontal") は左右（ADキー）の入力値
        Vector3 moveZ = cameraForward * Input.GetAxis("Vertical") * speed;  //　前後（カメラ基準）　 
        Vector3 moveX = cameraTrans.right * Input.GetAxis("Horizontal") * speed; // 左右（カメラ基準）

        // 地面にいるときはジャンプを可能に
        if (Input.GetKeyDown(KeyCode.Space) && groundChek.CheckGroundStatus())
        {
            moveDirection.y = jump;
        }
        else
        {
            moveDirection.y = 0;
        }

        // プレイヤーの向きを入力の向きに変更　
        transform.LookAt(transform.position + moveZ + moveX);

        moveDirection = moveZ + moveX + new Vector3(0, moveDirection.y, 0);
        // Move は指定したベクトルだけ移動させる命令
        rigitBody.position = new Vector3(rigitBody.position.x + moveDirection.x * Time.deltaTime, rigitBody.position.y + moveDirection.y * Time.deltaTime, rigitBody.position.z + moveDirection.z * Time.deltaTime);

        if (friend != null)
        {
            float dist = Vector3.Distance(this.transform.position, friend.transform.position);
            if (dist >= distance)
            {
                friend = null;
                chatStartObj.SetActive(false);
            }
        }
    }

    private void OnCollisionEnter(Collision hit)
    {
        friend = hit.gameObject.GetComponent<Friend>();
        //他のユーザーにぶつかればチャットするかをPlayerに尋ねる
        if (friend != null)
        {
            chatStartObj.SetActive(true);
        }
    }
}