using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [HideInInspector]
    public int id;

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;

    [HideInInspector]
    //public float curHatTime;
    public bool isDead = false;

    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;

    [PunRPC]
    public void Initialize (Player player)
    {
        photonPlayer = player;
        id = player.ActorNumber;

        GameManager.instance.players[id - 1] = this;

        if (id == 1)
        {
            GameManager.instance.GiveHat(id, true);
        }

        if (!photonView.IsMine)
            rig.isKinematic = true;
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (GameManager.instance.curHatTime >= GameManager.instance.timeToExplode && isDead == false)
            {
                
                isDead = true;
                GameManager.instance.playersAlive -= 1;

                if (GameManager.instance.playersAlive <= 1 && !GameManager.instance.gameEnded)
                {
                    //end the game
                    GameManager.instance.gameEnded = true;
                    GameManager.instance.photonView.RPC("WinGame", RpcTarget.All, id);
                }
                else if (GameManager.instance.playersAlive > 1 && !GameManager.instance.gameEnded)
                {
                    // new bomb
                    for (int x = 0; x < GameManager.instance.players.Length; ++x)
                    {
                        if (GameManager.instance.players[x].isDead == false)
                        {
                            GameManager.instance.curHatTime = 0;
                            GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All, GameManager.instance.players[x].id, false);
                        }
                    }
                }
            }
        }


        if (photonView.IsMine)
        {
            if (isDead == false)
            {
                Move();

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    TryJump();
                }

                if (hatObject.activeInHierarchy)
                {
                    GameManager.instance.curHatTime += Time.deltaTime;
                }
            }
        }
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal") * moveSpeed;
        float z = Input.GetAxis("Vertical") * moveSpeed;

        rig.velocity = new Vector3(x, rig.velocity.y, z);
    }

    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, 0.7f))
        {
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    public void SetHat (bool HasHat)
    {
        hatObject.SetActive(HasHat);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.instance.GetPlayer(collision.gameObject).id == GameManager.instance.playerWithHat)
            {
                if (GameManager.instance.CanGetHat())
                {
                    GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All, id, false);
                }
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(GameManager.instance.curHatTime);
        }
        else if (stream.IsReading)
        {
            GameManager.instance.curHatTime = (float)stream.ReceiveNext();
        }
    }
}
