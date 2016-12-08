
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerHandler : MonoBehaviour
{
    //Role
    public enum Role
    {
        Runner,
        Chaser
    }

    //PowerUp Hold
    public enum PowerUp_State
    {
        None,
        Darkball,
        DropObstacle,
        Shield,
        Speed
    }

    private int count = 4;
    //Variables
    public bool isShielded = false;
    public bool rewardIsActive = false;
    public int obAvoids = 0;
    public int maxAvoids = 3;
    public int curAvoids = 0;
    public float maxRewardCountdown;
    public float rewardCountdown;
    public Role myRole;
    public PowerUp_State myPowerUp;

    //Gameobjects
    private GameObject gameManager;
    public GameObject myModel;

    //Components
    private Rigidbody myRigidbody;
    private PlayerController player;
    private Animator myAnimator;

    //platform
    private Transform NormalPlatform;
    float timer = 0f;

    //Models
    public GameObject modelDagger;
    public GameObject modelLantern;

    public ParticleSystem possession;

    //Powerups
    public GameObject darkball;
    public GameObject dropObs;
    
    public Text powerupIndicate;
    public GameObject powerupEffectPlayer;

    public KeyCode usePowerUp;

    //Sounds
    private AudioSource[] collisionSFX;
    private AudioSource[] dodgeAudioSources;

    private AudioSource slopeDownsource;
    private AudioSource slopeUpsource;

    private AudioSource platformSlow;
    private AudioSource platformFast;

    private AudioSource ObsStunSFX;


    private AudioSource dodgedOb;
    private AudioSource powerUpGet;
    private AudioSource powerupUse;

    //UI
    public Image[] powerUpCountImg;

    //Effect
    public GameObject powerupEffect;
    public GameObject powerupEffect2;

    void Start()
    {
        //init components
        myRigidbody = GetComponent<Rigidbody>();
        player = GetComponent<PlayerController>();
        myAnimator = myModel.GetComponent<Animator>();

        //get gameobjects
        gameManager = GameObject.Find("Game_Manager");
        Debug.Log(gameManager);
        //init variables
        rewardCountdown = maxRewardCountdown;

        //sounds
        collisionSFX = GameObject.Find("CollisionSFX").GetComponents<AudioSource>();
        dodgeAudioSources = GameObject.Find("PlayerRewardSFX").GetComponents<AudioSource>();
        slopeDownsource = collisionSFX[0];
        slopeUpsource = collisionSFX[1];

        platformFast = collisionSFX[2];
        platformSlow = collisionSFX[3];

        ObsStunSFX = collisionSFX[4];

        dodgedOb = dodgeAudioSources[0];
        powerUpGet = dodgeAudioSources[1];
        powerupUse = dodgeAudioSources[2];

        possession = GetComponentInChildren<ParticleSystem>();
        possession.enableEmission = false;

        //Disable models
        modelDagger.GetComponent<MeshRenderer>().enabled = false;
        modelLantern.GetComponent<SkinnedMeshRenderer>().enabled = false;
    }

    //Wall collide detection
    void FixedUpdate()
    {
        // Get the velocity
        Vector3 horizontalMove = myRigidbody.velocity;
        // Don't use the vertical velocity
        horizontalMove.y = 0;
        // Calculate the approximate distance that will be traversed
        float distance = horizontalMove.magnitude * Time.fixedDeltaTime;
        // Normalize horizontalMove since it should be used to indicate direction
        horizontalMove.Normalize();
        RaycastHit hit;

        // Check if the body's current velocity will result in a collision
        if (myRigidbody.SweepTest(horizontalMove, out hit, distance) && hit.transform.gameObject.tag == "Wall")
        {
            // If so, stop the movement
            myRigidbody.velocity = new Vector3(0, myRigidbody.velocity.y, 0);
        }
    }

    void Update()
    {
        //CheckPowerUpReward();
        haloforchaser();
        SwitchRole_ItemModels();
        powerUpState();
    }

    void SwitchRole_ItemModels()
    {
        myAnimator.SetInteger("MyRole", (int)myRole);
        switch (gameManager.GetComponent<Game_Manager>().curState)
        {
            case Game_Manager.GameState.Phase1_Pause:
                {
                    modelDagger.GetComponent<MeshRenderer>().enabled = false;
                    modelLantern.GetComponent<SkinnedMeshRenderer>().enabled = false;
                    break;
                }
            case Game_Manager.GameState.Phase2_Pause:
                {
                    switch (myRole)
                    {
                        case Role.Runner:
                            {
                                modelLantern.GetComponent<SkinnedMeshRenderer>().enabled = true;
                                break;
                            }
                        case Role.Chaser:
                            {
                                modelDagger.GetComponent<MeshRenderer>().enabled = true;
                                break;
                            }
                    }
                    break;
                }
        }
    }

    void powerUpState()
    {
        switch (myPowerUp)
        {
            case PowerUp_State.Darkball:
                {
                    StopCoroutine(useDarkBall());
                    StartCoroutine(useDarkBall());
                    break;
                }
            case PowerUp_State.DropObstacle:
                {
                    StopCoroutine(useDropObs());
  
                    StartCoroutine(useDropObs());
  
                    break;
                }
            case PowerUp_State.Shield:
                {
 
                   StartCoroutine(powerupPlayerEffect());
                   StopCoroutine(PowerUpShield());
                   StartCoroutine(PowerUpShield());
 
                   break;
               }
           case PowerUp_State.Speed:
               {
                   StartCoroutine(powerupPlayerEffect());
                   StopCoroutine(PowerUpSpeed());
                   StartCoroutine(PowerUpSpeed());
 
                   break;
               }
 
        }
 
    }

    void CheckPowerUpReward()
    {
        if (rewardIsActive)
        {
            rewardCountdown -= Time.deltaTime;
            if (rewardCountdown < 0)
            {
                //Reset
                rewardIsActive = false;
            }
        }
        else
        {
            curAvoids = 0;
            rewardCountdown = maxRewardCountdown;
        }
    }

    void OnTriggerEnter(Collider col)
    {
        //OBSTACLES////////////////////////////////////
        //Slow
        if (col.gameObject.tag == "obSlow")
        {
            if (!isShielded)
            {
                //reset
                rewardIsActive = false;
                StopCoroutine(Obstacle_Slow());
                StartCoroutine(Obstacle_Slow());

                platformSlow.Play();
            }
        }

        //Stun
        if (col.gameObject.tag == "obStun")
        {
            if (!isShielded)
            {
                //reset
                ObsStunSFX.Play();

                rewardIsActive = false;
                StopCoroutine(Obstacle_Stun());
                StartCoroutine(Obstacle_Stun());
                Destroy(col.gameObject, 0.5f);
            }
            Destroy(col.gameObject);
        }

        //Check if avoided obstacles
        if (col.gameObject.tag == "Avoided" || col.gameObject.tag == "Reward")
        {
            //Check if reward timer is not active
            if (!rewardIsActive && myPowerUp == PowerUp_State.None)
            {
                rewardIsActive = true;
            }

            //Check if reward timer is active
            if (rewardIsActive)
            {
                powerUpCountImg[curAvoids].gameObject.SetActive(true);

                curAvoids++;                //increment the number of avoided obstacles
                if (curAvoids >= maxAvoids) //if the number of avoided obstacles have past the max
                {
                    myPowerUp = (PowerUp_State)Random.Range(1, 4);
                    powerUpGet.Play();//play powerup get
                    curAvoids = 0;

                    StartCoroutine(powerUpIE());
                    powerupIndicate.gameObject.SetActive(true);
                    powerUpCountImg[0].gameObject.SetActive(false);
                    powerUpCountImg[1].gameObject.SetActive(false);
                    powerUpCountImg[2].gameObject.SetActive(false);

                    powerUpGet.Play();

                    myPowerUp = (PowerUp_State)Random.Range(1, 4);
                    StartCoroutine(powerUpIndicateIE());

                    StartCoroutine(powerupTime());
                    curAvoids = 0;

                    //reset
                    rewardIsActive = false;
                }
            }

            dodgedOb.Play();//play dodge ob
            
        }

        //platform(ground) slow and fast
        if (col.gameObject.tag == "Platformslow")
        {
            if (player.movSpeed > 0f)
            {
                Debug.Log("slow");
                platformSlow.Play();
                platformSlow.Play();
                player.movSpeed = player.maxSpeed - 1f;
            }
        }

        if (col.gameObject.tag == "Platformreallyslow")
        {
            if (player.movSpeed > 0f)
            {
                Debug.Log("slow");

                platformSlow.Play();

                player.movSpeed = player.maxSpeed - 5f;
            }
        }
        if (col.gameObject.tag == "Platformfast")
        {
            if (player.movSpeed > 0f)
            {
                Debug.Log("fast");

                platformFast.Play();
                player.movSpeed = player.maxSpeed + 3f;
            }
        }
        if (col.gameObject.tag == "Platformreallyfast")
        {
            if (player.movSpeed > 0f)
            {
                Debug.Log("fast");
                platformFast.Play();

                platformFast.Play();
                player.movSpeed = player.maxSpeed + 5f;
            }
        }
    }

    //stay on platform
    void OnCollisionStay(Collision col)
    {
        //Handle platforms
        if (col.gameObject.tag == "Platformmoving")
        {
            if (timer <= 2f)
            {
                this.transform.position = col.transform.position;
            }
        }
    }

    //handle Incline
    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Incline")
        {
            CheckGroundIncline(col.gameObject);
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.gameObject.tag == "Incline")
        {
            StopCoroutine(NormGround());
            StartCoroutine(NormGround());
        }        
    }

    //incline check
    void CheckGroundIncline(GameObject ground)
    {
        //get angle from game obj
        Vector3 angle = ground.transform.eulerAngles;
        Vector3 ParentAngle = ground.transform.parent.gameObject.transform.eulerAngles;

        //check for a slow inc
        if (ParentAngle.y <= 1f)
            if (angle.z >= 5f && angle.z <= 45f && ParentAngle.y == 0f)
            {
                slopeUpsource.Play();
                StopCoroutine(SlowGround());
                StartCoroutine(SlowGround());

            }
        //check for a fast inc
        if (ParentAngle.y >= 179f)
        {
            if (angle.z >= 5f && angle.z <= 45f)
            {
                slopeDownsource.Play();
                StopCoroutine(FastGround());
                StartCoroutine(FastGround());
            }
        }
    }

    IEnumerator SlowGround()//Incline slow check
    {
        Debug.Log("SLOW INCLINE: " + player.movSpeed);
        player.movSpeed = player.maxSpeed - 4f;
        yield return null;
    }

    IEnumerator FastGround()//Incline fast check
    {
        Debug.Log("FAST INCLINE: " + player.movSpeed);
        player.movSpeed = player.maxSpeed + 4f;
        yield return null;
    }

    IEnumerator NormGround()//Flat ground check
    {
        player.movSpeed = player.maxSpeed;
        yield return null;
    }

    IEnumerator powerUpIE()
    {
        powerupEffect.gameObject.SetActive(true);
        powerupEffect2.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        powerupEffect.gameObject.SetActive(false);
        powerupEffect2.gameObject.SetActive(false);
    }

    IEnumerator powerupTime()
    {
        yield return new WaitForSeconds(4f);
        myPowerUp = PowerUp_State.None;
    }

    IEnumerator powerUpIndicateIE()
    {
        string s = powerupIndicate.text;
        if(myPowerUp == PowerUp_State.Darkball)
        {
            powerupIndicate.text = powerupIndicate.text + " Dark Ball!";
        }
        else if(myPowerUp == PowerUp_State.DropObstacle)
        {
            powerupIndicate.text = powerupIndicate.text + " Drop Obstacle!";
        }
        else if(myPowerUp == PowerUp_State.Shield)
        {
            powerupIndicate.text = "Shield!";
        }
        else if(myPowerUp == PowerUp_State.Speed)
        {
            powerupIndicate.text = "Speed UP!";
        }

        yield return new WaitForSeconds(4f);
        powerupIndicate.text = s;
        powerupIndicate.gameObject.SetActive(false);
    }

    IEnumerator PowerUpShield()
    {
        isShielded = true;
        yield return new WaitForSeconds(3f);
        isShielded = false;
    }

    void PowerUpDropObstacle()
    {
        Instantiate(dropObs, new Vector3(transform.position.x - 2.5f, transform.position.y + 1f, transform.position.z), Quaternion.identity);
    }
    
    void PowerUpDarkBall()
    {
        Instantiate(darkball, new Vector3(transform.position.x + 1f, transform.position.y + 1.5f, transform.position.z), Quaternion.identity);
    }

    IEnumerator useDarkBall()
    {
        if(Input.GetKeyDown(usePowerUp))
        {
            powerupUse.Play();
            PowerUpDarkBall();
        }

        yield return new WaitForSeconds(4f);
    }

    //max 3 slow obstacles
    IEnumerator useDropObs()
    {
        if(Input.GetKeyDown(usePowerUp))
        {
            count--;

            if(count > 0)
            {
                powerupUse.Play();

                PowerUpDropObstacle();
            }
        }

        yield return new WaitForSeconds(5f);
        count = 4;
    }

    IEnumerator darkballHit()
    {
        if(darkball.gameObject.transform.position == this.gameObject.transform.position)
        {
            if(player.movSpeed > 0)
            {
                player.movSpeed = player.maxSpeed - 2f;
            }
        }

        yield return new WaitForSeconds(5f);
        player.movSpeed = player.maxSpeed;
    }

    //Speed
    IEnumerator PowerUpSpeed()
    {
        player.movSpeed = player.maxSpeed + 5f;
        yield return new WaitForSeconds(3f);
        player.movSpeed = player.maxSpeed;
    }

    IEnumerator powerupPlayerEffect()
    {
        powerupEffectPlayer.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        powerupEffectPlayer.gameObject.SetActive(false);
    }

    //Obstacles

    //Slow
    IEnumerator Obstacle_Slow()
    {
        player.movSpeed = player.maxSpeed - 5f;
        yield return new WaitForSeconds(2f);
        player.movSpeed = player.maxSpeed;
    }

    //Stun
    IEnumerator Obstacle_Stun()
    {
        player.movSpeed = 0f;
        yield return new WaitForSeconds(0.5f);
        player.movSpeed = player.maxSpeed;
    }

    void haloforchaser()
    {
        if (myRole == Role.Chaser)
        {
            possession.enableEmission = true;
        }
    }
}