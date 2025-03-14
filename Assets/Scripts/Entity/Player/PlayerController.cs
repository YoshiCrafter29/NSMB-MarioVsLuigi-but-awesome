using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

using Photon.Pun;
using ExitGames.Client.Photon;
using NSMB.Utils;
using static UnityEngine.InputSystem.InputAction;

public class PlayerController : MonoBehaviourPun, IFreezableEntity, ICustomSerializeView, IOnPhotonViewPreNetDestroy {

    #region Variables

    // == NETWORKING VARIABLES ==
    private static readonly float EPSILON = 0.2f, RESEND_RATE = 0.5f;

    public bool Active { get; set; } = true;
    private Vector2 previousJoystick;
    private short previousFlags;
    private byte previousFlags2;
    private float lastSendTimestamp;

    // == MONOBEHAVIOURS ==

    public int playerId = -1;
    public bool dead = false, spawned = false;
    public Enums.PowerupState state = Enums.PowerupState.Small, previousState;
    public float slowriseGravity = 0.85f, normalGravity = 2.5f, flyingGravity = 0.8f, flyingTerminalVelocity = 1.25f, drillVelocity = 7f, groundpoundTime = 0.25f, groundpoundVelocity = 10, blinkingSpeed = 0.25f, terminalVelocity = -7f, jumpVelocity = 6.25f, megaJumpVelocity = 16f, launchVelocity = 12f, walkingAcceleration = 8f, runningAcceleration = 3f, walkingMaxSpeed = 2.7f, runningMaxSpeed = 5, wallslideSpeed = -4.25f, walljumpVelocity = 5.6f, giantStartTime = 1.5f, soundRange = 10f, slopeSlidingAngle = 12.5f, pickupTime = 0.5f;
    public float propellerLaunchVelocity = 6, propellerFallSpeed = 2, propellerSpinFallSpeed = 1.5f, propellerSpinTime = 0.75f, propellerDrillBuffer, heightSmallModel = 0.42f, heightLargeModel = 0.82f;

    BoxCollider2D[] hitboxes;
    GameObject models;

    public CameraController cameraController;
    public FadeOutManager fadeOut;
    public SpecialBehaviour specialBehaviour = SpecialBehaviour.NONE;

    public AudioSource sfx, sfxBrick;
    private Animator animator;
    public Rigidbody2D body;

    public PlayerAnimationController AnimationController { get; private set; }

    public bool onGround, previousOnGround, crushGround, doGroundSnap, jumping, properJump, hitRoof, skidding, turnaround, facingRight = true, singlejump, doublejump, triplejump, bounce, crouching, groundpound, sliding, knockback, hitBlock, running, functionallyRunning, jumpHeld, flying, drill, inShell, hitLeft, hitRight, iceSliding, stuckInBlock, alreadyStuckInBlock, propeller, usedPropellerThisJump, stationaryGiantEnd, fireballKnockback, startedSliding, groundpounded, canShootProjectile;
    public float jumpLandingTimer, landing, koyoteTime, groundpoundCounter, groundpoundStartTimer, pickupTimer, groundpoundDelay, hitInvincibilityCounter, powerupFlash, throwInvincibility, jumpBuffer, giantStartTimer, giantEndTimer, propellerTimer, propellerSpinTimer, fireballTimer;
    public float invincible, giantTimer, floorAngle, knockbackTimer, pipeTimer, slowdownTimer;

    //Walljumping variables
    private float wallSlideTimer, wallJumpTimer;
    public bool wallSlideLeft, wallSlideRight;

    private int _starCombo;
    public int StarCombo {
        get => (invincible > 0 ? _starCombo : 0);
        set => _starCombo = (invincible > 0 ? value : 0);
    }

    public Vector2 pipeDirection;
    public int stars, coins, lives = -1;
    public Powerup storedPowerup = null;
    public HoldableEntity holding, holdingOld;
    public FrozenCube frozenObject;

    private bool powerupButtonHeld;
    private readonly float analogDeadzone = 0.35f;
    public Vector2 joystick, giantSavedVelocity, previousFrameVelocity, previousFramePosition;

    public GameObject onSpinner;
    public PipeManager pipeEntering;
    public DoorManager doorEntering;
    public float doorEnteringTime = 0f;
    public int doorEnteringStep = 0;
    public bool step, alreadyGroundpounded;
    private int starDirection;
    public PlayerData character;

    //Tile data
    private Enums.Sounds footstepSound = Enums.Sounds.Player_Walk_Grass;
    public bool doIceSkidding;
    private float tileFriction = 1;
    private readonly HashSet<Vector3Int> tilesStandingOn = new(),
        tilesJumpedInto = new(),
        tilesHitSide = new();

    private GameObject trackIcon;

    private Hashtable gameState = new() {
        [Enums.NetPlayerProperties.GameState] = new Hashtable()
    }; //only used to update joining spectators

    private bool initialKnockbackFacingRight = false;

    // == FREEZING VARIABLES ==
    public bool Frozen { get; set; }
    bool IFreezableEntity.IsCarryable => true;
    bool IFreezableEntity.IsFlying => flying || propeller; //doesn't work consistently?


    public BoxCollider2D MainHitbox => hitboxes[0];
    public Vector2 WorldHitboxSize => MainHitbox.size * transform.lossyScale;

    public AudioSource megachad;

    #endregion

    #region -- OPPRESSOR MKII STUFF --
    // this is so stupid lmao
    public float oppressorAngle = 0f;
    [HideInInspector] public float oppressorMaxSpeed = 8.5f;
    public float oppressorSpeed = 0f;
    public bool oppressorMoving = false;

    public void HandleOppressorPhysics(bool left, bool right, bool crouch, bool up, bool jump)
    {

        float oppressorVelocity = (body.velocity.x) / Mathf.Cos(Mathf.Deg2Rad * oppressorAngle);
        oppressorAngle = Mathf.Lerp(oppressorAngle, ((up ? -45f : 0) + (crouch ? 45f : 0)), 0.25f * Time.deltaTime * 60);
        body.velocity = new Vector3(oppressorSpeed = Mathf.Lerp(oppressorVelocity, (oppressorMoving = !(left == right)) ? (oppressorMaxSpeed * (right ? 1f : -1f)) : 0f, 0.0625f * Time.deltaTime * 15), 0f, 0f);

        if (left || right) facingRight = right;
        HandleCoins();
        HandleStuckInBlock();

        body.velocity = new Vector3(
            body.velocity.x * Mathf.Cos(Mathf.Deg2Rad * oppressorAngle),
            Mathf.Abs(body.velocity.x) * -Mathf.Sin(Mathf.Deg2Rad * oppressorAngle),
            0f);
        
    }
    #endregion
    #region -- SERIALIZATION / EVENTS --
    public void Serialize(List<byte> buffer) {
        bool updateJoystick = Vector2.Distance(joystick, previousJoystick) > EPSILON;

        SerializationUtils.PackToShort(out short flags, running, jumpHeld, crouching, groundpound,
                facingRight, onGround, knockback, flying, drill, sliding, skidding, wallSlideLeft,
                wallSlideRight, invincible > 0, propellerSpinTimer > 0, wallJumpTimer > 0);
        SerializationUtils.PackToByte(out byte flags2);
        bool updateFlags = flags != previousFlags || flags2 != previousFlags2;

        bool forceResend = PhotonNetwork.Time - lastSendTimestamp > RESEND_RATE;

        if (forceResend || updateJoystick || updateFlags) {
            //send joystick for simulation reasons
            SerializationUtils.PackToShort(buffer, joystick, -1, 1);
            previousJoystick = joystick;

            //serialize movement flags
            SerializationUtils.WriteShort(buffer, flags);
            previousFlags = flags;
            //SerializationUtils.WriteByte(buffer, flags2);
            //previousFlags2 = flags2;

            lastSendTimestamp = (float) PhotonNetwork.Time;
        }
    }

    public void Deserialize(List<byte> buffer, ref int index, PhotonMessageInfo info) {
        //controller position
        SerializationUtils.UnpackFromShort(buffer, ref index, -1, 1, out joystick);

        //controller flags
        SerializationUtils.UnpackFromShort(buffer, ref index, out bool[] flags);
        running = flags[0];
        jumpHeld = flags[1];
        crouching = flags[2];
        groundpound = flags[3];
        facingRight = flags[4];
        previousOnGround = doGroundSnap = onGround = flags[5];
        knockback = flags[6];
        flying = flags[7];
        drill = flags[8];
        sliding = flags[9];
        skidding = flags[10];
        wallSlideLeft = flags[11];
        wallSlideRight = flags[12];
        invincible = flags[13] ? 1 : 0;
        propellerSpinTimer = flags[14] ? 1 : 0;
        wallJumpTimer = flags[15] ? 1 : 0;

        //SerializationUtils.UnpackFromByte(buffer, ref index, out bool[] flags2);

        //resimulations
        float lag = (float) (PhotonNetwork.Time - info.SentServerTime);
        int fullResims = (int) (lag / Time.fixedDeltaTime);
        float partialResim = lag % Time.fixedDeltaTime;

        while (fullResims-- > 0)
            HandleMovement(Time.fixedDeltaTime);
        HandleMovement(partialResim);
    }

    #endregion

    #region -- START / UPDATE --
    public void Awake() {
        cameraController = GetComponent<CameraController>();
        cameraController.controlCamera = photonView.IsMineOrLocal();

        animator = GetComponentInChildren<Animator>();
        body = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        sfxBrick = GetComponents<AudioSource>()[1];
        //hitboxManager = GetComponent<WrappingHitbox>();
        AnimationController = GetComponent<PlayerAnimationController>();
        fadeOut = GameObject.FindGameObjectWithTag("FadeUI").GetComponent<FadeOutManager>();


        megachad = gameObject.AddComponent<AudioSource>();
        megachad.clip = Resources.Load("Sound/music/gigachad") as AudioClip;
        megachad.loop = true;
        megachad.spatialize = true;
        megachad.spatialBlend = 1.0f;
        megachad.minDistance = 2.5f;
        megachad.maxDistance = 7.5f;
        megachad.rolloffMode = AudioRolloffMode.Logarithmic;
        megachad.playOnAwake = false;

        body.position = transform.position = GameManager.Instance.GetSpawnpoint(playerId);

        models = transform.Find("Models").gameObject;
        starDirection = Random.Range(0, 4);

        int count = 0;
        foreach (var player in PhotonNetwork.PlayerList) {

            Utils.GetCustomProperty(Enums.NetPlayerProperties.Spectator, out bool spectating, photonView.Owner.CustomProperties);
            if (spectating)
                continue;

            if (player == photonView.Owner)
                break;
            count++;
        }

        playerId = count;
        Utils.GetCustomProperty(Enums.NetRoomProperties.Lives, out lives);

        if (photonView.IsMine) {
#if UNITY_ANDROID
            GameManager.Instance.androidUI.SetActive(true);
            GameManager.Instance.controlRun.stateChange += OnPowerupAction;
            GameManager.Instance.controlRun.stateChange += OnSprint;
            GameManager.Instance.controlJump.stateChange += OnJump;
#else
            GameManager.Instance.androidUI.SetActive(false);
            InputSystem.controls.Player.Movement.performed += OnMovement;
            InputSystem.controls.Player.Jump.performed += OnJump;
            InputSystem.controls.Player.Sprint.started += OnSprint;
            InputSystem.controls.Player.Sprint.canceled += OnSprint;
            InputSystem.controls.Player.PowerupAction.performed += OnPowerupAction;
            InputSystem.controls.Player.ReserveItem.performed += OnReserveItem;
#endif
        }

        GameManager.Instance.allPlayers.Add(this);
    }

    public void OnPreNetDestroy(PhotonView rootView) {
        GameManager.Instance.allPlayers.Remove(this);
    }

    public void Start() {
        hitboxes = GetComponents<BoxCollider2D>();
        trackIcon = UIUpdater.Instance.CreatePlayerIcon(this);
        transform.position = body.position = GameManager.Instance.spawnpoint;
        cameraController.Recenter();

        LoadFromGameState();
    }

    public void OnDestroy() {
        if (!photonView.IsMine)
            return;

#if !UNITY_ANDROID
            InputSystem.controls.Player.Movement.performed -= OnMovement;
            InputSystem.controls.Player.Jump.performed -= OnJump;
            InputSystem.controls.Player.Sprint.started -= OnSprint;
            InputSystem.controls.Player.Sprint.canceled -= OnSprint;
            InputSystem.controls.Player.PowerupAction.performed -= OnPowerupAction;
            InputSystem.controls.Player.ReserveItem.performed -= OnReserveItem;
#endif
    }

    [PunRPC]
    public void UpdateCharacter(int charID)
    {
        character = GlobalController.Instance.characters[charID];
    }
    public void OnGameStart() {
        photonView.RPC("PreRespawn", RpcTarget.All);

        gameState = new() {
            [Enums.NetPlayerProperties.GameState] = new Hashtable()
        };
    }

    public void LoadFromGameState() {

        //Don't load from our own state
        if (photonView.IsMine)
            return;

        //We don't have a state to load
        if (photonView.Owner.CustomProperties[Enums.NetPlayerProperties.GameState] is not Hashtable gs)
            return;

        lives = (int) gs[Enums.NetPlayerGameState.Lives];
        stars = (int) gs[Enums.NetPlayerGameState.Stars];
        coins = (int) gs[Enums.NetPlayerGameState.Coins];
        state = (Enums.PowerupState) gs[Enums.NetPlayerGameState.PowerupState];
        if (gs[Enums.NetPlayerGameState.ReserveItem] != null) {
            storedPowerup = (Powerup) Resources.Load("Scriptables/Powerups/" + (Enums.PowerupState) gs[Enums.NetPlayerGameState.ReserveItem]);
        } else {
            storedPowerup = null;
        }
    }

    public void UpdateGameState() {
        if (!photonView.IsMine)
            return;

        UpdateGameStateVariable(Enums.NetPlayerGameState.Lives, lives);
        UpdateGameStateVariable(Enums.NetPlayerGameState.Stars, stars);
        UpdateGameStateVariable(Enums.NetPlayerGameState.Coins, coins);
        UpdateGameStateVariable(Enums.NetPlayerGameState.PowerupState, (byte) state);
        UpdateGameStateVariable(Enums.NetPlayerGameState.ReserveItem, storedPowerup ? storedPowerup.state : null);

        photonView.Owner.SetCustomProperties(gameState);
    }

    private void UpdateGameStateVariable(string key, object value) {
        ((Hashtable) gameState[Enums.NetPlayerProperties.GameState])[key] = value;
    }

    public void FixedUpdate() {
        //game ended, freeze.
        if (!GameManager.Instance.musicEnabled) {
            models.SetActive(false);
            return;
        }
        if (GameManager.Instance.gameover) {
            body.velocity = Vector2.zero;
            animator.enabled = false;
            body.isKinematic = true;
            return;
        }

#if UNITY_ANDROID
        // android stuff
        if (photonView.IsMine)
        {
            joystick = new Vector2(
                (GameManager.Instance.controlLeft.pressed ? -1 : 0) + (GameManager.Instance.controlRight.pressed ? 1 : 0),
                (GameManager.Instance.controlUp.pressed ? 1 : 0) + (GameManager.Instance.controlDown.pressed ? -1 : 0)
            );
        }
#endif

        previousOnGround = onGround;
        if (!dead && !pipeEntering) {
            if (doorEntering)
            {
                HandleDoorEntering();
            } else
            {
                HandleBlockSnapping();
                bool snapped = state == Enums.PowerupState.OppressorMKII ? false : GroundSnapCheck();
                HandleGroundCollision();
                onGround |= snapped;
                doGroundSnap = onGround;
                HandleTileProperties();
                TickCounters();
                HandleMovement(Time.fixedDeltaTime);
                HandleGiantTiles(true);
                HandleCoins();
                UpdateHitbox();
            }
        }
        if (holding && holding.dead)
            holding = null;

        AnimationController.UpdateAnimatorStates();
        HandleLayerState();
        previousFrameVelocity = body.velocity;
        previousFramePosition = body.position;
    }
#endregion

#region -- COLLISIONS --

    void HandleCoins()
    {

        Vector2 checkSize = WorldHitboxSize * 1.1f;

        bool grounded = previousFrameVelocity.y < -8f && onGround;
        Vector2 offset = Vector2.zero;
        if (grounded)
            offset = Vector2.down / 2f;

        Vector2 checkPosition = body.position + (Vector2.up * checkSize * 0.5f) + (2 * Time.fixedDeltaTime * body.velocity) + offset;

        Vector3Int minPos = Utils.WorldToTilemapPosition(checkPosition - (checkSize * 0.5f), wrap: false);
        Vector3Int size = Utils.WorldToTilemapPosition(checkPosition + (checkSize * 0.5f), wrap: false) - minPos;

        for (int x = 0; x <= size.x; x++)
        {
            for (int y = 0; y <= size.y; y++)
            {
                Vector3Int tileLocation = new(minPos.x + x, minPos.y + y, 0);
                Vector2 worldPosCenter = Utils.TilemapToWorldPosition(tileLocation) + Vector3.one * 0.25f;
                Utils.WrapTileLocation(ref tileLocation);

                SpinningCoinTile tile = GameManager.Instance.tilemap.GetTile<SpinningCoinTile>(tileLocation);

                if (tile)
                {
                    CollectCoin(-1, worldPosCenter, tile.coins);
                    object[] parametersTile = new object[] { tileLocation.x, tileLocation.y, null };
                    GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.SetTile, parametersTile, ExitGames.Client.Photon.SendOptions.SendReliable);
                }
            }
        }

        
    }
    void HandleGroundCollision() {
        tilesJumpedInto.Clear();
        tilesStandingOn.Clear();
        tilesHitSide.Clear();

        bool ignoreRoof = false;
        int down = 0, left = 0, right = 0, up = 0;

        crushGround = false;
        foreach (BoxCollider2D hitbox in hitboxes) {
            ContactPoint2D[] contacts = new ContactPoint2D[20];
            int collisionCount = hitbox.GetContacts(contacts);

            for (int i = 0; i < collisionCount; i++) {
                ContactPoint2D contact = contacts[i];
                GameObject go = contact.collider.gameObject;
                Vector2 n = contact.normal;
                Vector2 p = contact.point + (contact.normal * -0.15f);
                if (n == Vector2.up && contact.point.y > body.position.y)
                    continue;

                Vector3Int vec = Utils.WorldToTilemapPosition(p);
                if (!contact.collider || contact.collider.CompareTag("Player"))
                    continue;

                if (Vector2.Dot(n, Vector2.up) > .05f) {
                    if (Vector2.Dot(body.velocity.normalized, n) > 0.1f && !onGround) {
                        if (!contact.rigidbody || contact.rigidbody.velocity.y < body.velocity.y)
                            //invalid flooring
                            continue;
                    }

                    crushGround |= !go.CompareTag("platform") && !go.CompareTag("frozencube");
                    down++;
                    tilesStandingOn.Add(vec);
                } else if (contact.collider.gameObject.layer == Layers.LayerGround) {
                    if (Vector2.Dot(n, Vector2.down) > .9f) {
                        up++;
                        tilesJumpedInto.Add(vec);
                    } else {
                        if (n.x < 0) {
                            right++;
                        } else {
                            left++;
                        }
                        tilesHitSide.Add(vec);
                    }
                }
            }
        }

        onGround = down >= 1;
        hitLeft = left >= 1;
        hitRight = right >= 1;
        hitRoof = !ignoreRoof && up > 1;
    }
    void HandleTileProperties() {
        doIceSkidding = false;
        tileFriction = -1;
        footstepSound = Enums.Sounds.Player_Walk_Grass;
        foreach (Vector3Int pos in tilesStandingOn) {
            TileBase tile = Utils.GetTileAtTileLocation(pos);
            if (tile == null)
                continue;
            if (tile is TileWithProperties propTile) {
                footstepSound = propTile.footstepSound;
                doIceSkidding = propTile.iceSkidding;
                tileFriction = Mathf.Max(tileFriction, propTile.frictionFactor);
            } else {
                tileFriction = 1;
            }
        }
        if (tileFriction == -1)
            tileFriction = 1;
    }

    private ContactPoint2D[] contacts = new ContactPoint2D[0];
    public void OnCollisionStay2D(Collision2D collision) {
        if (!photonView.IsMine || (knockback && !fireballKnockback) || Frozen)
            return;

        GameObject obj = collision.gameObject;

        switch (collision.gameObject.tag) {
        case "Player": {
            //hit players

            if (contacts.Length < collision.contactCount)
                contacts = new ContactPoint2D[collision.contactCount];
            collision.GetContacts(contacts);

            foreach (ContactPoint2D contact in contacts) {
                GameObject otherObj = collision.gameObject;
                PlayerController other = otherObj.GetComponent<PlayerController>();
                PhotonView otherView = other.photonView;

                if (other.invincible > 0) {
                    //They are invincible. let them decide if they've hit us.
                    if (invincible > 0) {
                        //oh, we both are. bonk.
                        photonView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x > body.position.x, 0, true, otherView.ViewID);
                        other.photonView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x < body.position.x, 0, true, photonView.ViewID);
                    }
                    return;
                }

                if (invincible > 0) {
                    //we are invincible. murder time :)
                    if (other.state == Enums.PowerupState.MegaMushroom) {
                        //wait fuck-
                        photonView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x > body.position.x, 0, true, otherView.ViewID);
                        return;
                    }

                    otherView.RPC("Powerdown", RpcTarget.All, false);
                    body.velocity = previousFrameVelocity;
                    return;
                }

                float dot = Vector2.Dot((body.position - other.body.position).normalized, Vector2.up);
                bool above = dot > 0.7f;
                bool otherAbove = dot < -0.7f;

                //mega mushroom cases
                if (state == Enums.PowerupState.MegaMushroom || other.state == Enums.PowerupState.MegaMushroom) {
                    if (state == Enums.PowerupState.MegaMushroom && other.state == Enums.PowerupState.MegaMushroom) {
                        //both giant
                        if (above) {
                            bounce = true;
                            groundpound = false;
                            drill = false;
                            otherView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, false, photonView.ViewID);
                        } else if (!otherAbove) {
                            otherView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x < body.position.x, 0, true, photonView.ViewID);
                            photonView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x > body.position.x, 0, true, otherView.ViewID);
                        }
                    } else if (state == Enums.PowerupState.MegaMushroom) {
                        //only we are giant
                        otherView.RPC("Powerdown", RpcTarget.All, false);
                        body.velocity = previousFrameVelocity;
                    }
                    return;
                }

                //blue shell cases
                if (inShell) {
                    //we are blue shell
                    if (!otherAbove) {
                        //hit them. powerdown them
                        if (other.inShell) {
                            //collide with both
                            otherView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, true, photonView.ViewID);
                            photonView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x > body.position.x, 1, true, otherView.ViewID);
                        } else {
                            otherView.RPC("Powerdown", RpcTarget.All, false);
                        }
                        float dotRight = Vector2.Dot((body.position - other.body.position).normalized, Vector2.right);
                        facingRight = dotRight > 0;
                        return;
                    }
                }
                if (state == Enums.PowerupState.BlueShell && otherAbove && (!other.groundpound && !other.drill) && (crouching || groundpound)) {
                    body.velocity = new(runningMaxSpeed * 0.9f * (otherObj.transform.position.x < body.position.x ? 1 : -1), body.velocity.y);
                }
                if (other.inShell && !above)
                    return;

                if (!above && other.state == Enums.PowerupState.BlueShell && !other.inShell && other.crouching && !groundpound && !drill) {
                    //they are blue shell
                    bounce = true;
                    photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
                    return;
                }

                if (above) {
                    //hit them from above
                    bounce = !groundpound && !drill;
                    bool groundpounded = groundpound || drill;

                    if (state == Enums.PowerupState.MiniMushroom && other.state != Enums.PowerupState.MiniMushroom) {
                        //we are mini, they arent. special rules.
                        if (groundpounded) {
                            otherView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, false, photonView.ViewID);
                            groundpound = false;
                            bounce = true;
                        } else {
                            photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
                        }
                    } else if (other.state == Enums.PowerupState.MiniMushroom && groundpounded) {
                        //we are big, groundpounding a mini opponent. squish.
                        otherView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x > body.position.x, 3, false, photonView.ViewID);
                        bounce = false;
                    } else {
                        if (other.state == Enums.PowerupState.MiniMushroom && groundpounded) {
                            otherView.RPC("Powerdown", RpcTarget.All, false);
                        } else {
                            otherView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x < body.position.x, groundpounded ? 3 : 1, false, photonView.ViewID);
                        }
                    }
                    body.velocity = new Vector2(previousFrameVelocity.x, body.velocity.y);

                    return;
                } else if (!knockback && !other.knockback && !otherAbove && onGround && other.onGround && (Mathf.Abs(previousFrameVelocity.x) > walkingMaxSpeed || Mathf.Abs(other.previousFrameVelocity.x) > walkingMaxSpeed)) {
                    //bump

                    otherView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, true, photonView.ViewID);
                    photonView.RPC("Knockback", RpcTarget.All, otherObj.transform.position.x > body.position.x, 1, true, otherView.ViewID);
                }
            }
            break;
        }
        case "MarioBrosPlatform": {
            List<Vector2> points = new();
            foreach (ContactPoint2D c in collision.contacts) {
                if (c.normal != Vector2.down)
                    continue;

                points.Add(c.point);
            }
            if (points.Count == 0)
                return;

            Vector2 avg = new();
            foreach (Vector2 point in points)
                avg += point;
            avg /= points.Count;

            obj.GetPhotonView().RPC("Bump", RpcTarget.All, photonView.ViewID, avg);
            break;
        }
        case "frozencube": {
            if (holding == obj || (holdingOld == obj && throwInvincibility > 0))
                return;

            obj.GetComponent<FrozenCube>().InteractWithPlayer(this);
            break;
        }
        }
    }

    public void OnTriggerEnter2D(Collider2D collider) {
        if (!photonView.IsMine || dead || Frozen || pipeEntering || doorEntering || !MainHitbox.IsTouching(collider))
            return;

        HoldableEntity holdable = collider.GetComponentInParent<HoldableEntity>();
        if (holdable && (holding == holdable || (holdingOld == holdable && throwInvincibility > 0)))
            return;

        KillableEntity killable = collider.GetComponentInParent<KillableEntity>();
        if (killable && !killable.dead && !killable.Frozen) {
            killable.InteractWithPlayer(this);
            return;
        }

        GameObject obj = collider.gameObject;
        switch (obj.tag) {
        case "Fireball": {
            FireballMover fireball = obj.GetComponentInParent<FireballMover>();
            if (fireball.photonView.IsMine || hitInvincibilityCounter > 0)
                return;

            fireball.photonView.RPC("Kill", RpcTarget.All);

            if (knockback || invincible > 0 || state == Enums.PowerupState.MegaMushroom)
                return;

            if (state == Enums.PowerupState.BlueShell && (inShell || crouching || groundpound)) {
                if (fireball.isIceball) {
                    //slowdown
                    slowdownTimer = 0.65f;
                }
                return;
            }

            if (state == Enums.PowerupState.MiniMushroom) {
                photonView.RPC("Powerdown", RpcTarget.All, false);
                return;
            }

            if (!fireball.isIceball) {
                photonView.RPC("Knockback", RpcTarget.All, fireball.left, 1, true, fireball.photonView.ViewID);
            } else {
                if (!Frozen && !frozenObject && !pipeEntering && !doorEntering) {
                    GameObject cube = PhotonNetwork.Instantiate("Prefabs/FrozenCube", transform.position, Quaternion.identity, 0, new object[] { photonView.ViewID });
                    frozenObject = cube.GetComponent<FrozenCube>();
                    return;
                }
            }
            break;
        }
        case "lava":
        case "poison": {
            if (!photonView.IsMine)
                return;
            photonView.RPC("Death", RpcTarget.All, false, obj.CompareTag("lava"));
            return;
        }
        }

        OnTriggerStay2D(collider);
    }

    protected void OnTriggerStay2D(Collider2D collider) {
        GameObject obj = collider.gameObject;
        if (obj.CompareTag("spinner")) {
            onSpinner = obj;
            return;
        }

        if (!photonView.IsMine || dead || Frozen)
            return;

        switch (obj.tag) {
        case "Powerup": {
            if (!photonView.IsMine)
                return;
            MovingPowerup powerup = obj.GetComponentInParent<MovingPowerup>();
            if (powerup.followMeCounter > 0 || powerup.ignoreCounter > 0)
                break;
            photonView.RPC("Powerup", RpcTarget.AllViaServer, powerup.photonView.ViewID);
            Destroy(collider);
            break;
        }
        case "bigstar":
            photonView.RPC("CollectBigStar", RpcTarget.AllViaServer, obj.transform.parent.gameObject.GetPhotonView().ViewID);
            break;
        case "loosecoin":
            Transform parent = obj.transform.parent;
            photonView.RPC("CollectCoin", RpcTarget.AllViaServer, parent.gameObject.GetPhotonView().ViewID, parent.position, 1);
            break;
        case "coin":
            photonView.RPC("CollectCoin", RpcTarget.AllViaServer, obj.GetPhotonView().ViewID, new Vector3(obj.transform.position.x, collider.transform.position.y, 0), 1);
            break;
        }
    }

    protected void OnTriggerExit2D(Collider2D collider) {
        if (collider.CompareTag("spinner"))
            onSpinner = null;
    }
#endregion

#region -- CONTROLLER FUNCTIONS --
    public void OnMovement(InputAction.CallbackContext context) {
        if (!photonView.IsMine || GameManager.Instance.paused)
            return;

        joystick = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context) {
        OnJump(context.ReadValue<float>() >= 0.5f);
    }

    public void OnJump(bool jumpHeld)
    {
        if (!photonView.IsMine || GameManager.Instance.paused)
            return;

        this.jumpHeld = jumpHeld;

        if (jumpHeld)
            jumpBuffer = 0.15f;
    }

    public void OnSprint(InputAction.CallbackContext context) {

        OnSprint(context.started);
    }

    public void OnSprint(bool running)
    {
        if (!photonView.IsMine || GameManager.Instance.paused)
            return;

        if (Frozen)
            return;

        this.running = running;

        if (running && GlobalController.Instance.settings.fireballFromSprint)
            ActivatePowerupAction();
    }

    public void OnPowerupAction(InputAction.CallbackContext context) {
        OnPowerupAction(context.ReadValue<float>() >= 0.5f);
    }

    public void OnPowerupAction(bool powerupButtonHeld)
    {
        if (!photonView.IsMine || dead || GameManager.Instance.paused)
            return;
        if (!powerupButtonHeld)
            return;

        ActivatePowerupAction();
    }

    private void ActivatePowerupAction() {
        if (knockback || pipeEntering || doorEntering || GameManager.Instance.gameover || dead || Frozen || holding)
            return;

        switch (state) {
            case Enums.PowerupState.IceFlower:
            case Enums.PowerupState.McDonalds:
            case Enums.PowerupState.BombFlower:
            case Enums.PowerupState.FireFlower:
            case Enums.PowerupState.OppressorMKII:
            case Enums.PowerupState.CoinFlower: {
                    if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
                        return;

                    int count = 0;
                    Utils.GetCustomProperty(Enums.NetRoomProperties.ChaosMode, out bool chaos);

                    foreach (FireballMover existingFire in FindObjectsOfType<FireballMover>()) {
                        if (existingFire.photonView.IsMine && ++count >= (chaos ? 35 : 6))
                            return;
                    }

                    if (state == Enums.PowerupState.McDonalds)
                    {
                        canShootProjectile = true;
                    } else
                    {
                        if (count <= 1)
                        {
                            fireballTimer = 1.25f;
                            canShootProjectile = count == 0;
                        }
                        else if (fireballTimer <= 0)
                        {
                            fireballTimer = 1.25f;
                            canShootProjectile = true;
                        }
                        else if (canShootProjectile)
                        {
                            canShootProjectile = false;
                        }
                        else
                        {
                            return;
                        }
                    }

                    string projectile = "";
                    string wallProjectile = "FireballWall";
                    Enums.Sounds sound = Enums.Sounds.Enemy_Bobomb_Explode;
                    switch (state) {
                        case Enums.PowerupState.FireFlower:
                            projectile = "Fireball";
                            sound = Enums.Sounds.Powerup_Fireball_Shoot;
                            break;
                        case Enums.PowerupState.IceFlower:
                            projectile = "IceBall";
                            sound = Enums.Sounds.Powerup_Iceball_Shoot;
                            wallProjectile = "IceBallWall";
                            break;
                        case Enums.PowerupState.McDonalds:
                            projectile = "Hamburger";
                            sound = Enums.Sounds.Powerup_BigMac;
                            wallProjectile = "HamburgerWall";
                            break;
                        case Enums.PowerupState.BombFlower:
                            projectile = "BobombProj";
                            //wallProjectile = "BobombProj";
                            sound = Enums.Sounds.Powerup_Fireball_Shoot;
                            break;
                        case Enums.PowerupState.CoinFlower:
                            projectile = "LooseCoin";
                            wallProjectile = "../LooseCoin";
                            sound = Enums.Sounds.Powerup_Fireball_Shoot;
                            break;
                        case Enums.PowerupState.OppressorMKII:
                            projectile = "missile";
                            break;
                    }

            Vector2 pos = body.position + new Vector2(facingRight ^ animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround") ? 0.5f : -0.5f, 0.3f);
            if (state != Enums.PowerupState.BombFlower && Utils.IsTileSolidAtWorldLocation(pos)) {
                 photonView.RPC("SpawnParticle", RpcTarget.All, $"Prefabs/Particle/{wallProjectile}", pos);
            } else {
                GameObject g = PhotonNetwork.Instantiate($"Prefabs/{projectile}", pos, Quaternion.identity, 0, new object[] { !facingRight ^ animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround"), body.velocity.x });
                if (g.GetComponent<MissileMover>() is MissileMover m && m)
                {
                    m.playerController = this;
                    m.transform.rotation = Quaternion.Euler(0f, 0f, (facingRight ? -90f : 90f) - (oppressorAngle));
                    m.speed += Mathf.Abs(oppressorSpeed);
                }
            }
            photonView.RPC("PlaySound", RpcTarget.All, sound);

            animator.SetTrigger("fireball");
            break;
        }
            case Enums.PowerupState.PropellerMushroom: {
                if (groundpound || (flying && drill) || propeller || crouching || sliding || wallJumpTimer > 0)
                    return;

                photonView.RPC("StartPropeller", RpcTarget.All);
                break;
            }
        }
    }

    [PunRPC]
    protected void StartPropeller() {
        if (usedPropellerThisJump)
            return;

        body.velocity = new Vector2(body.velocity.x, propellerLaunchVelocity);
        propellerTimer = 1f;
        PlaySound(Enums.Sounds.Powerup_PropellerMushroom_Start);

        animator.SetTrigger("propeller_start");
        propeller = true;
        flying = false;
        crouching = false;

        singlejump = false;
        doublejump = false;
        triplejump = false;

        wallSlideLeft = false;
        wallSlideRight = false;

        if (onGround) {
            onGround = false;
            doGroundSnap = false;
            body.position += Vector2.up * 0.15f;
        }
        usedPropellerThisJump = true;
    }

    public void OnReserveItem(bool reserved)
    {
        if (!photonView.IsMine || GameManager.Instance.paused || GameManager.Instance.gameover)
            return;

        if (storedPowerup == null || dead || !spawned)
        {
            PlaySound(Enums.Sounds.UI_Error);
            return;
        }

        SpawnReserveItem();
    }
    public void OnReserveItem(InputAction.CallbackContext context) {
        OnReserveItem(true);
    }
#endregion

#region -- POWERUP / POWERDOWN --
    [PunRPC]
    protected void Powerup(int actor, PhotonMessageInfo info) {
        PhotonView view;
        if (dead || !(view = PhotonView.Find(actor)))
            return;

        MovingPowerup powerupObj = view.GetComponent<MovingPowerup>();
        if (powerupObj.followMeCounter > 0)
            return;

        Powerup powerup = powerupObj.powerupScriptable;
        Enums.PowerupState newState = powerup.state;
        Enums.PriorityPair pp = Enums.PowerupStatePriority[powerup.state];
        Enums.PriorityPair cp = Enums.PowerupStatePriority[state];
        bool reserve = cp.statePriority > pp.itemPriority || state == newState;
        bool soundPlayed = false;

        if (newState == Enums.PowerupState.OppressorMKII)
        {
            oppressorAngle = 0f;
        }
        if (powerup.state == Enums.PowerupState.MegaMushroom && state != Enums.PowerupState.MegaMushroom) {

            giantStartTimer = giantStartTime;
            knockback = false;
            groundpound = false;
            crouching = false;
            propeller = false;
            usedPropellerThisJump = false;
            flying = false;
            drill = false;
            inShell = false;
            giantTimer = 15f;
            transform.localScale = Vector3.one;
            Instantiate(Resources.Load("Prefabs/Particle/GiantPowerup"), transform.position, Quaternion.identity);

            PlaySoundEverywhere(powerup.soundEffect);
            soundPlayed = true;

        } else if (powerup.prefab == "Star") {
            //starman
            if (invincible <= 0)
                StarCombo = 0;

            invincible = 10f;
            PlaySound(powerup.soundEffect);

            if (holding && photonView.IsMine) {
                holding.photonView.RPC("SpecialKill", RpcTarget.All, facingRight, false, 0);
                holding = null;
            }

            if (view.IsMine)
                PhotonNetwork.Destroy(view);
            Destroy(view.gameObject);

            return;
        } else if (powerup.prefab == "1-Up") {
            lives++;
            UpdateGameState();
            PlaySound(powerup.soundEffect);
            Instantiate(Resources.Load("Prefabs/Particle/1Up"), transform.position, Quaternion.identity);

            if (view.IsMine)
                PhotonNetwork.Destroy(view);
            Destroy(view.gameObject);

            return;
        } else if (state == Enums.PowerupState.MiniMushroom) {
            //check if we're in a mini area to avoid crushing ourselves
            if (onGround && Physics2D.Raycast(body.position, Vector2.up, 0.3f, Layers.MaskOnlyGround)) {
                reserve = true;
            }
        }

        if (reserve)
        {
            if (storedPowerup == null || (storedPowerup != null && Enums.PowerupStatePriority[storedPowerup.state].statePriority <= pp.statePriority && !(state == Enums.PowerupState.Mushroom && newState != Enums.PowerupState.Mushroom)))
            {
                //dont reserve mushrooms
                storedPowerup = powerup;
            }
            PlaySound(Enums.Sounds.Player_Sound_PowerupReserveStore);
        }
        else
        {
            if (!(state == Enums.PowerupState.Mushroom && newState != Enums.PowerupState.Mushroom) && (storedPowerup == null || Enums.PowerupStatePriority[storedPowerup.state].statePriority <= cp.statePriority)) {
                storedPowerup = (Powerup) Resources.Load("Scriptables/Powerups/" + state);
            }
            if (state == Enums.PowerupState.OppressorMKII)
                previousState = newState;
            else
            {
                previousState = state;
                state = newState;
            }
            powerupFlash = 2;
            crouching |= ForceCrouchCheck();
            propeller = false;
            usedPropellerThisJump = false;
            drill &= flying;
            propellerTimer = 0;

            if (!soundPlayed)
                PlaySound(powerup.soundEffect);
        }

        UpdateGameState();

        if (view.IsMine)
            PhotonNetwork.Destroy(view);
        Destroy(view.gameObject);

        //hitboxManager.Update();
    }

    [PunRPC]
    protected void Powerdown(bool ignoreInvincible) {
        if (!ignoreInvincible && (hitInvincibilityCounter > 0 || invincible > 0))
            return;

        if (Enums.PowerupState.OppressorMKII != state) previousState = state;
        bool nowDead = false;

        switch (state) {
            case Enums.PowerupState.OppressorMKII:
                state = previousState;
                SpawnStars(1, false);

                if (photonView.IsMine)
                {
                    // launch oppressor
                    GameObject e = PhotonNetwork.InstantiateRoomObject("Prefabs/Powerup/Mushroom", body.position, Quaternion.identity);

                    MovingPowerup p = e.GetComponent<MovingPowerup>();
                    p.ignoreCounter = 1f;
                    e.GetComponent<Rigidbody2D>().AddForce(new Vector2(5f, 5f));
                }
                break;
            case Enums.PowerupState.MiniMushroom:
            case Enums.PowerupState.Small: {
                if (photonView.IsMine)
                    photonView.RPC("Death", RpcTarget.All, false, false);
                nowDead = true;
                break;
            }
            case Enums.PowerupState.Mushroom: {
                state = Enums.PowerupState.Small;
                powerupFlash = 2f;
                SpawnStars(1, false);
                break;
            }
            default: {
                state = Enums.PowerupState.Mushroom;
                powerupFlash = 2f;
                SpawnStars(1, false);
                break;
            }
        }
        propeller = false;
        propellerTimer = 0;
        propellerSpinTimer = 0;
        usedPropellerThisJump = false;

        if (!nowDead) {
            hitInvincibilityCounter = 3f;
            PlaySound(Enums.Sounds.Player_Sound_Powerdown);
        }
    }
#endregion

#region -- FREEZING --
    [PunRPC]
    public void Freeze(int cube) {
        if (knockback || hitInvincibilityCounter > 0 || invincible > 0 || Frozen || state == Enums.PowerupState.MegaMushroom)
            return;

        PlaySound(Enums.Sounds.Enemy_Generic_Freeze);
        frozenObject = PhotonView.Find(cube).GetComponentInChildren<FrozenCube>();
        Frozen = true;
        frozenObject.autoBreakTimer = 1.75f;
        animator.enabled = false;
        body.isKinematic = true;
        body.simulated = false;
        knockback = false;
        skidding = false;
        drill = false;
        wallSlideLeft = false;
        wallSlideRight = false;
        propeller = false;

        propellerTimer = 0;
        skidding = false;
    }

    [PunRPC]
    public void Unfreeze(byte reasonByte) {
        if (!Frozen)
            return;

        Frozen = false;
        animator.enabled = true;
        body.simulated = true;
        body.isKinematic = false;

        bool doKnockback = reasonByte != (byte) IFreezableEntity.UnfreezeReason.Timer;

        if (frozenObject && frozenObject.photonView.IsMine) {
            frozenObject.holder?.photonView.RPC("Knockback", RpcTarget.All, frozenObject.holder.facingRight, 1, true, photonView.ViewID);
            frozenObject.Kill();
        }

        if (doKnockback)
            Knockback(facingRight, 1, true, -1);
        else
            hitInvincibilityCounter = 1.5f;
    }
#endregion

#region -- COIN / STAR COLLECTION --
    [PunRPC]
    protected void CollectBigStar(int starID) {
        PhotonView view = PhotonView.Find(starID);
        if (!view)
            return;

        GameObject star = view.gameObject;
        StarBouncer starScript = star.GetComponent<StarBouncer>();
        if (!starScript.Collectable)
            return;

        if (photonView.IsMine && starScript.stationary)
            GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.ResetTiles, null, SendOptions.SendReliable);

        stars = Mathf.Min(stars + 1, GameManager.Instance.starRequirement);

        UpdateGameState();
        GameManager.Instance.CheckForWinner();

        Instantiate(Resources.Load("Prefabs/Particle/StarCollect"), star.transform.position, Quaternion.identity);
        PlaySoundEverywhere(photonView.IsMine ? Enums.Sounds.World_Star_Collect_Self : Enums.Sounds.World_Star_Collect_Enemy);

        if (view.IsMine)
            PhotonNetwork.Destroy(view);
        DestroyImmediate(star);
        DestroyImmediate(star);
    }

    public void kill()
    {
        if (photonView.IsMine)
            photonView.RPC("Death", RpcTarget.All, false, false);
    }

    [PunRPC]
    protected void CollectCoin(int coinID, Vector3 position, int amount = 1) {
        if (coinID != -1) {
            PhotonView coinView = PhotonView.Find(coinID);
            if (!coinView)
                return;

            GameObject coin = coinView.gameObject;
            if (coin.CompareTag("loosecoin")) {
                if (coin.GetPhotonView().IsMine)
                    PhotonNetwork.Destroy(coin);
                DestroyImmediate(coin);
            } else {
                SpriteRenderer renderer = coin.GetComponent<SpriteRenderer>();
                if (!renderer.enabled)
                    return;
                renderer.enabled = false;
                coin.GetComponent<BoxCollider2D>().enabled = false;
            }
        }
        Instantiate(Resources.Load("Prefabs/Particle/CoinCollect"), position, Quaternion.identity);

        PlaySound(Enums.Sounds.World_Coin_Collect);
        NumberParticle num = ((GameObject) Instantiate(Resources.Load("Prefabs/Particle/Number"), position, Quaternion.identity)).GetComponentInChildren<NumberParticle>();
        num.text.text = Utils.GetSymbolString((coins + 1).ToString(), Utils.numberSymbols);
        num.color = AnimationController.GlowColor;

        if (state == Enums.PowerupState.Suit)
            coins += amount + Mathf.RoundToInt(Mathf.Pow(Random.value, 2f) * 2f);
        else
            coins += amount;
        
        if (coins >= GameManager.Instance.coinRequirement) {
            SpawnCoinItem();
            coins %= GameManager.Instance.coinRequirement;
        }

        UpdateGameState();
    }

    public void SpawnReserveItem() {
        if (storedPowerup == null)
            return;

        string prefab = storedPowerup.prefab;
        GameObject e = PhotonNetwork.Instantiate("Prefabs/Powerup/" + prefab, body.position + Vector2.up * 5f, Quaternion.identity, 0, new object[] { photonView.ViewID });
        MushroomAutoFollow followThing = e.GetComponent<MushroomAutoFollow>();
        if (followThing != null)
            followThing.playerWhoSpawnedIt = this.gameObject;

        photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Sound_PowerupReserveUse);
        storedPowerup = null;
        UpdateGameState();
    }

    public void SpawnCoinItem() {
        if (coins < GameManager.Instance.coinRequirement)
            return;

        if (!photonView.IsMine)
            return;

        string prefab = Utils.GetRandomItem(this).prefab;
        PhotonNetwork.Instantiate("Prefabs/Powerup/" + prefab, body.position + Vector2.up * 5f, Quaternion.identity, 0, new object[] { photonView.ViewID });
        photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Sound_PowerupReserveUse);
    }

    void SpawnStars(int amount, bool deathplane) {

        if (!photonView.IsMine)
            return;

        bool fastStars = amount > 2 && stars > 2;

        while (amount > 0) {
            if (stars <= 0)
                break;

            if (!fastStars) {
                if (starDirection == 0)
                    starDirection = 2;
                if (starDirection == 3)
                    starDirection = 1;
            }
            SpawnStar(deathplane);
            stars--;
            amount--;
        }
        GameManager.Instance.CheckForWinner();
        UpdateGameState();
    }

    void SpawnStar(bool deathplane) {
        PhotonNetwork.Instantiate("Prefabs/BigStar", body.position + Vector2.up * transform.localScale * MainHitbox.size, Quaternion.identity, 0, new object[] { starDirection, photonView.ViewID, PhotonNetwork.ServerTimestamp + 1000, deathplane });

        starDirection = (starDirection + 1) % 4;
    }
#endregion

#region -- DEATH / RESPAWNING --
    [PunRPC]
    protected void Death(bool deathplane, bool fire) {
        if (dead)
            return;

        //if (info.Sender != photonView.Owner)
        //    return;

        switch(specialBehaviour)
        {
            case SpecialBehaviour.MCQUEEN:
                if (photonView.IsMine)
                {
                    Instantiate(Resources.Load("Prefabs/Particle/Explosion", typeof(GameObject)), transform.position, Quaternion.identity);
                    AnimationController.models.SetActive(false);
                }
                break;
            default:
                animator.Play("deadstart");
                break;
        }
        if (--lives == 0) {
            GameManager.Instance.CheckForWinner();
        }
        if (deathplane)
            spawned = false;
        dead = true;
        onSpinner = null;
        pipeEntering = null;
        doorEntering = null;
        inShell = false;
        propeller = false;
        propellerSpinTimer = 0;
        flying = false;
        drill = false;
        sliding = false;
        crouching = false;
        skidding = false;
        turnaround = false;
        groundpound = false;
        knockback = false;
        wallSlideLeft = false;
        wallSlideRight = false;
        animator.SetBool("knockback", false);
        animator.SetBool("flying", false);
        animator.SetBool("firedeath", fire);
        PlaySound(Enums.Sounds.Player_Sound_Death);
        SpawnStars(groundpounded ? 3 : 1, deathplane);
        body.isKinematic = false;
        groundpounded = false;
        if (holding) {
            holding.photonView.RPC("Throw", RpcTarget.All, !facingRight, true);
            holding = null;
        }
        holdingOld = null;

        if (photonView.IsMine)
            ScoreboardUpdater.instance.OnDeathToggle();
    }

    [PunRPC]
    public void PreRespawn() {

        sfx.enabled = true;
        if (lives == 0) {
            GameManager.Instance.CheckForWinner();
            Destroy(trackIcon);
            if (photonView.IsMine) {
                PhotonNetwork.Destroy(photonView);
                GameManager.Instance.SpectationManager.Spectating = true;
            }
            return;
        }
        transform.localScale = Vector2.one;
        transform.position = body.position = GameManager.Instance.GetSpawnpoint(playerId);
        dead = false;
        cameraController.Recenter();
        previousState = state = Enums.PowerupState.Small;
        AnimationController.DisableAllModels();
        spawned = false;
        animator.SetTrigger("respawn");
        invincible = 0;
        giantTimer = 0;
        giantEndTimer = 0;
        giantStartTimer = 0;
        groundpound = false;
        body.isKinematic = false;

        GameObject particle = (GameObject) Instantiate(Resources.Load("Prefabs/Particle/Respawn"), body.position, Quaternion.identity);
        particle.GetComponent<RespawnParticle>().player = this;

        gameObject.SetActive(false);

        if (GameManager.Instance.localPlayer == this.gameObject && GameManager.Instance.mainMusic != GameManager.Instance._mainMusic)
        {
            GameManager.Instance.musicState = null;
            GameManager.Instance.PlaySong(Enums.MusicState.Normal, GameManager.Instance._mainMusic);
        }
    }


    [PunRPC]
    public void Respawn(PhotonMessageInfo info) {

        gameObject.SetActive(true);
        dead = false;
        spawned = true;
        state = Enums.PowerupState.Small;
        previousState = Enums.PowerupState.Small;
        body.velocity = Vector2.zero;
        wallSlideLeft = false;
        wallSlideRight = false;
        wallSlideTimer = 0;
        wallJumpTimer = 0;
        flying = false;

        propeller = false;
        propellerSpinTimer = 0;
        usedPropellerThisJump = false;
        propellerTimer = 0;

        crouching = false;
        onGround = false;
        sliding = false;
        koyoteTime = 1f;
        jumpBuffer = 0;
        invincible = 0;
        giantStartTimer = 0;
        giantTimer = 0;
        singlejump = false;
        doublejump = false;
        turnaround = false;
        triplejump = false;
        knockback = false;
        bounce = false;
        skidding = false;
        groundpound = false;
        inShell = false;
        landing = 0f;
        ResetKnockback();
        Instantiate(Resources.Load("Prefabs/Particle/Puff"), transform.position, Quaternion.identity);
        models.transform.rotation = Quaternion.Euler(0, 180, 0);

        if (photonView.IsMine)
            ScoreboardUpdater.instance.OnRespawnToggle();

        UpdateGameState();
    }
#endregion

#region -- SOUNDS / PARTICLES --
    [PunRPC]
    public void PlaySoundEverywhere(Enums.Sounds sound) {
        GameManager.Instance.sfx.PlayOneShot(sound.GetClip(character));
    }
    [PunRPC]
    public void PlaySound(Enums.Sounds sound, byte variant, float volume) {
        if (sound == Enums.Sounds.Powerup_MegaMushroom_Break_Block) {
            sfxBrick.Stop();
            sfxBrick.clip = sound.GetClip(character, variant);
            sfxBrick.Play();
        } else {
            sfx.PlayOneShot(sound.GetClip(character, variant), volume);
        }
    }
    [PunRPC]
    public void PlaySound(Enums.Sounds sound, byte variant) {
        PlaySound(sound, variant, 1);
    }
    [PunRPC]
    public void PlaySound(Enums.Sounds sound) {
        PlaySound(sound, 0, 1);
    }

    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos) {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.identity);
    }

    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos, Vector3 rot) {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.Euler(rot));
    }

    protected void GiantFootstep() {
        CameraController.ScreenShake = 0.15f;
        SpawnParticle("Prefabs/Particle/GroundpoundDust", body.position + new Vector2(facingRight ? 0.5f : -0.5f, 0));
        PlaySound(Enums.Sounds.Powerup_MegaMushroom_Walk, (byte) (step ? 1 : 2));
        step = !step;
    }

    protected void Footstep() {
        if (state == Enums.PowerupState.MegaMushroom)
            return;
        if (doIceSkidding && skidding) {
            PlaySound(Enums.Sounds.World_Ice_Skidding);
            return;
        }
        if (propeller) {
            PlaySound(Enums.Sounds.Powerup_PropellerMushroom_Kick);
            return;
        }
        if (Mathf.Abs(body.velocity.x) < walkingMaxSpeed)
            return;

        PlaySound(footstepSound, (byte) (step ? 1 : 2), Mathf.Abs(body.velocity.x) / (runningMaxSpeed + 4));
        step = !step;
    }
#endregion

#region -- TILE COLLISIONS --
    void HandleGiantTiles(bool pipes)
    {
        if (state != Enums.PowerupState.MegaMushroom || !photonView.IsMine || giantStartTimer > 0)
            return;

        Vector2 checkSize = WorldHitboxSize * 1.1f;

        bool grounded = previousFrameVelocity.y < -8f && onGround;
        Vector2 offset = Vector2.zero;
        if (grounded)
            offset = Vector2.down / 2f;

        Vector2 checkPosition = body.position + (Vector2.up * checkSize * 0.5f) + (2 * Time.fixedDeltaTime * body.velocity) + offset;

        Vector3Int minPos = Utils.WorldToTilemapPosition(checkPosition - (checkSize * 0.5f), wrap: false);
        Vector3Int size = Utils.WorldToTilemapPosition(checkPosition + (checkSize * 0.5f), wrap: false) - minPos;

        for (int x = 0; x <= size.x; x++)
        {
            for (int y = 0; y <= size.y; y++)
            {
                Vector3Int tileLocation = new(minPos.x + x, minPos.y + y, 0);
                Vector2 worldPosCenter = Utils.TilemapToWorldPosition(tileLocation) + Vector3.one * 0.25f;
                Utils.WrapTileLocation(ref tileLocation);

                InteractableTile.InteractionDirection dir = InteractableTile.InteractionDirection.Up;
                if (worldPosCenter.y - 0.25f + Physics2D.defaultContactOffset * 2f <= body.position.y)
                {
                    if (!grounded && !groundpound)
                        continue;

                    dir = InteractableTile.InteractionDirection.Down;
                }
                else if (worldPosCenter.y + Physics2D.defaultContactOffset * 2f >= body.position.y + size.y)
                {
                    dir = InteractableTile.InteractionDirection.Up;
                }
                else if (worldPosCenter.x <= body.position.x)
                {
                    dir = InteractableTile.InteractionDirection.Left;
                }
                else if (worldPosCenter.x >= body.position.x)
                {
                    dir = InteractableTile.InteractionDirection.Right;
                }

                BreakablePipeTile pipe = GameManager.Instance.tilemap.GetTile<BreakablePipeTile>(tileLocation);
                if (pipe && (pipe.upsideDownPipe || !pipes || groundpound))
                    continue;

                InteractWithTile(tileLocation, dir);
            }
        }
        if (pipes)
        {
            for (int x = 0; x <= size.x; x++)
            {
                for (int y = size.y; y >= 0; y--)
                {
                    Vector3Int tileLocation = new(minPos.x + x, minPos.y + y, 0);
                    Vector2 worldPosCenter = Utils.TilemapToWorldPosition(tileLocation) + Vector3.one * 0.25f;
                    Utils.WrapTileLocation(ref tileLocation);

                    InteractableTile.InteractionDirection dir = InteractableTile.InteractionDirection.Up;
                    if (worldPosCenter.y - 0.25f + Physics2D.defaultContactOffset * 2f <= body.position.y)
                    {
                        if (!grounded && !groundpound)
                            continue;

                        dir = InteractableTile.InteractionDirection.Down;
                    }
                    else if (worldPosCenter.x - 0.25f < checkPosition.x - checkSize.x * 0.5f)
                    {
                        dir = InteractableTile.InteractionDirection.Left;
                    }
                    else if (worldPosCenter.x + 0.25f > checkPosition.x + checkSize.x * 0.5f)
                    {
                        dir = InteractableTile.InteractionDirection.Right;
                    }

                    BreakablePipeTile pipe = GameManager.Instance.tilemap.GetTile<BreakablePipeTile>(tileLocation);
                    if (!pipe || !pipe.upsideDownPipe || dir == InteractableTile.InteractionDirection.Up)
                        continue;

                    InteractWithTile(tileLocation, dir);
                }
            }
        }
    }

    int InteractWithTile(Vector3Int tilePos, InteractableTile.InteractionDirection direction) {
        if (!photonView.IsMine)
            return 0;

        TileBase tile = GameManager.Instance.tilemap.GetTile(tilePos);
        if (!tile)
            return 0;

        if (tile is InteractableTile it)
            return it.Interact(this, direction, Utils.TilemapToWorldPosition(tilePos)) ? 1 : 0;

        return 0;
    }
#endregion

#region -- KNOCKBACK --

    [PunRPC]
    protected void Knockback(bool fromRight, int starsToDrop, bool fireball, int attackerView) {
        if (fireball && fireballKnockback && knockback)
            return;
        if (knockback && !fireballKnockback)
            return;

        if (!GameManager.Instance.started || hitInvincibilityCounter > 0 || pipeEntering || Frozen || dead || giantStartTimer > 0 || giantEndTimer > 0)
            return;

        if (state == Enums.PowerupState.MiniMushroom && starsToDrop > 1 && photonView.IsMineOrLocal()) {
            groundpounded = true;
            photonView.RPC("Powerdown", RpcTarget.All, false);
            return;
        }

        if (knockback || fireballKnockback)
            starsToDrop = Mathf.Min(1, starsToDrop);

        knockback = true;
        knockbackTimer = 0.5f;
        fireballKnockback = fireball;
        initialKnockbackFacingRight = facingRight;

        PhotonView attacker = PhotonNetwork.GetPhotonView(attackerView);
        if (attackerView >= 0) {
            if (attacker)
                SpawnParticle("Prefabs/Particle/PlayerBounce", attacker.transform.position);

            if (fireballKnockback)
                PlaySound(Enums.Sounds.Player_Sound_Collision_Fireball, 0, 3);
            else
                PlaySound(Enums.Sounds.Player_Sound_Collision, 0, 3);
        }
        animator.SetBool("fireballKnockback", fireball);
        animator.SetBool("knockforwards", facingRight != fromRight);

        float megaVelo = (state == Enums.PowerupState.MegaMushroom ? 3 : 1);
        body.velocity = new Vector2(
            (fromRight ? -1 : 1) *
            3 *
            (starsToDrop + 1) *
            megaVelo *
            (fireball ? 0.7f : 1f),

            fireball ? 0 : 4
        );

        if (onGround && !fireball)
            body.position += Vector2.up * 0.15f;

        onGround = false;
        doGroundSnap = false;
        inShell = false;
        groundpound = false;
        flying = false;
        propeller = false;
        propellerTimer = 0;
        propellerSpinTimer = 0;
        sliding = false;
        drill = false;
        body.gravityScale = normalGravity;
        wallSlideLeft = wallSlideRight = false;

        SpawnStars(starsToDrop, false);
        HandleLayerState();
    }

    public void ResetKnockbackFromAnim() {
        if (photonView.IsMine)
            photonView.RPC("ResetKnockback", RpcTarget.All);
    }

    [PunRPC]
    protected void ResetKnockback() {
        hitInvincibilityCounter = state != Enums.PowerupState.MegaMushroom ? 2f : 0f;
        bounce = false;
        knockback = false;
        body.velocity = new(0, body.velocity.y);
        facingRight = initialKnockbackFacingRight;
    }
#endregion

#region -- ENTITY HOLDING --
    [PunRPC]
    protected void HoldingWakeup(PhotonMessageInfo info) {
        holding = null;
        holdingOld = null;
        throwInvincibility = 0;
        Powerdown(false);
    }
    [PunRPC]
    public void SetHolding(int view) {
        if (view == -1) {
            if (holding)
                holding.holder = null;
            holding = null;
            return;
        }
        holding = PhotonView.Find(view).GetComponent<HoldableEntity>();
        if (holding is FrozenCube) {
            animator.Play("head-pickup");
            animator.ResetTrigger("fireball");
            PlaySound(Enums.Sounds.Player_Voice_DoubleJump, 2);
            pickupTimer = 0;
        } else {
            pickupTimer = pickupTime;
        }
        animator.ResetTrigger("throw");
        animator.SetBool("holding", true);

        SetHoldingOffset();
    }
    [PunRPC]
    public void SetHoldingOld(int view) {
        if (view == -1) {
            holding = null;
            return;
        }
        PhotonView v = PhotonView.Find(view);
        if (v == null)
            return;
        holdingOld = v.GetComponent<HoldableEntity>();
        throwInvincibility = 0.15f;
    }
#endregion

    void HandleSliding(bool up, bool down) {
        startedSliding = false;
        if (groundpound) {
            if (onGround) {
                if (state == Enums.PowerupState.MegaMushroom) {
                    groundpound = false;
                    groundpoundCounter = 0.5f;
                    return;
                }
                if (!inShell && Mathf.Abs(floorAngle) >= slopeSlidingAngle) {
                    groundpound = false;
                    sliding = true;
                    alreadyGroundpounded = true;
                    body.velocity = new Vector2(-Mathf.Sign(floorAngle) * groundpoundVelocity, 0);
                    startedSliding = true;
                } else {
                    body.velocity = Vector2.zero;
                    if (!down || state == Enums.PowerupState.MegaMushroom) {
                        groundpound = false;
                        groundpoundCounter = state == Enums.PowerupState.MegaMushroom ? 0.4f : 0.25f;
                    }
                }
            }
            if (up && groundpoundCounter <= 0.05f) {
                groundpound = false;
                body.velocity = Vector2.down * groundpoundVelocity;
            }
        }
        if (!((facingRight && hitRight) || (!facingRight && hitLeft)) && crouching && Mathf.Abs(floorAngle) >= slopeSlidingAngle && !inShell && state != Enums.PowerupState.MegaMushroom) {
            sliding = true;
            crouching = false;
            alreadyGroundpounded = true;
        }
        if (sliding && onGround && Mathf.Abs(floorAngle) > slopeSlidingAngle) {
            float angleDeg = floorAngle * Mathf.Deg2Rad;

            bool uphill = Mathf.Sign(floorAngle) == Mathf.Sign(body.velocity.x);
            float speed = Time.fixedDeltaTime * 5f * (uphill ? Mathf.Clamp01(1f - (Mathf.Abs(body.velocity.x) / runningMaxSpeed)) : 4f);

            float newX = Mathf.Clamp(body.velocity.x - (Mathf.Sin(angleDeg) * speed), -(runningMaxSpeed * 1.3f), runningMaxSpeed * 1.3f);
            float newY = Mathf.Sin(angleDeg) * newX + 0.4f;
            body.velocity = new Vector2(newX, newY);

        }

        if (up || (Mathf.Abs(floorAngle) < slopeSlidingAngle && onGround && !down) || (facingRight && hitRight) || (!facingRight && hitLeft)) {
            sliding = false;
            //alreadyGroundpounded = false;
        }
    }

    void HandleSlopes() {
        if (!onGround) {
            floorAngle = 0;
            return;
        }

        RaycastHit2D hit = Physics2D.BoxCast(body.position + (Vector2.up * 0.05f), new Vector2((MainHitbox.size.x - Physics2D.defaultContactOffset * 2f) * transform.lossyScale.x, 0.1f), 0, body.velocity.normalized, (body.velocity * Time.fixedDeltaTime).magnitude, Layers.MaskAnyGround);
        if (hit) {
            //hit ground
            float angle = Vector2.SignedAngle(Vector2.up, hit.normal);
            if (Mathf.Abs(angle) > 89)
                return;

            float x = floorAngle != angle ? previousFrameVelocity.x : body.velocity.x;

            floorAngle = angle;

            float change = Mathf.Sin(angle * Mathf.Deg2Rad) * x * 1.25f;
            body.velocity = new Vector2(x, change);
            onGround = true;
            doGroundSnap = true;
        } else if (onGround) {
            hit = Physics2D.BoxCast(body.position + (Vector2.up * 0.05f), new Vector2((MainHitbox.size.x + Physics2D.defaultContactOffset * 3f) * transform.lossyScale.x, 0.1f), 0, Vector2.down, 0.3f, Layers.MaskAnyGround);
            if (hit) {
                float angle = Vector2.SignedAngle(Vector2.up, hit.normal);
                if (Mathf.Abs(angle) > 89)
                    return;

                float x = floorAngle != angle ? previousFrameVelocity.x : body.velocity.x;
                floorAngle = angle;

                float change = Mathf.Sin(angle * Mathf.Deg2Rad) * x * 1.25f;
                body.velocity = new Vector2(x, change);
                onGround = true;
                doGroundSnap = true;
            } else {
                floorAngle = 0;
            }
        }
    }

    void HandleLayerState() {
        bool hitsNothing = animator.GetBool("pipe") || dead || stuckInBlock || giantStartTimer > 0 || (giantEndTimer > 0 && stationaryGiantEnd);
        bool shouldntCollide = (hitInvincibilityCounter > 0 && invincible <= 0) || (knockback && !fireballKnockback);

        int layer = Layers.LayerDefault;
        if (hitsNothing) {
            layer = Layers.LayerHitsNothing;
        } else if (shouldntCollide) {
            layer = Layers.LayerPassthrough;
        }

        gameObject.layer = layer;
    }

    bool GroundSnapCheck() {
        if (dead || (body.velocity.y > 0 && !onGround) || !doGroundSnap || pipeEntering || gameObject.layer == Layers.LayerHitsNothing)
            return false;

        bool prev = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;
        RaycastHit2D hit = Physics2D.BoxCast(body.position + Vector2.up * 0.1f, new Vector2(WorldHitboxSize.x, 0.05f), 0, Vector2.down, 0.4f, Layers.MaskAnyGround);
        Physics2D.queriesStartInColliders = prev;
        if (hit) {
            body.position = new Vector2(body.position.x, hit.point.y + Physics2D.defaultContactOffset);
            return true;
        }
        return false;
    }

    #region -- DOORS --

    void DoorCheck()
    {
        if (!photonView.IsMine || pipeEntering != null || doorEntering != null || joystick.y < analogDeadzone || state == Enums.PowerupState.MegaMushroom || !onGround || knockback || inShell)
            return;


        foreach (RaycastHit2D hit in Physics2D.RaycastAll(body.position, Vector2.down, 0.1f))
        {
            GameObject obj = hit.transform.gameObject;
            if (!obj.CompareTag("Door"))
                continue;
            DoorManager man = obj.GetComponent<DoorManager>();
            photonView.RPC("OpenDoor", RpcTarget.All, man.doorID);
        }
    }
    [PunRPC]
    void OpenDoor(int doorID)
    {
        if (GameManager.Instance.doors.TryGetValue(doorID, out doorEntering))
        {
            ResetStuff();
            AnimationController.models.SetActive(false);
            doorEntering.OpenDoor();
            doorEnteringTime = 0f;
            doorEnteringStep = 0;
            if (!photonView.IsMine) return;
        }
    }

    void HandleDoorEntering()
    {
        doorEnteringTime += Time.fixedDeltaTime;
        switch(doorEnteringStep)
        {
            case 0:
                if (doorEnteringTime > 0.25f)
                {
                    doorEnteringStep = 1;
                    if (photonView.IsMine)
                        fadeOut.FadeOutAndIn(0.45f, 0.1f);
                }
                break;
            case 1:
                if (doorEnteringTime > 0.8f)
                {
                    doorEnteringStep = 2;
                    transform.position = doorEntering.otherDoor.spawnPos.position;
                    doorEntering.otherDoor.OpenDoor();
                    cameraController.Recenter();
                }
                
                break;
            case 2:
                if (doorEnteringTime > 1f)
                    doorEntering = null;
                break;
        }
    }
    #endregion

    #region -- PIPES --
    void DownwardsPipeCheck() {
        if (!photonView.IsMine || joystick.y > -analogDeadzone || state == Enums.PowerupState.MegaMushroom || !onGround || knockback || inShell)
            return;

        foreach (RaycastHit2D hit in Physics2D.RaycastAll(body.position, Vector2.down, 0.1f)) {
            GameObject obj = hit.transform.gameObject;
            if (!obj.CompareTag("pipe"))
                continue;
            PipeManager pipe = obj.GetComponent<PipeManager>();
            if (pipe.miniOnly && state != Enums.PowerupState.MiniMushroom)
                continue;
            if (!pipe.entryAllowed)
                continue;
            if (pipe.entryType != Enums.PipeEntryType.AllowAll && pipe.playerList != null)
            {
                bool isAllowed = false;
                foreach (string p in pipe.playerList)
                {
                    if (p.ToLower() == photonView.Owner.NickName.ToLower())
                    {
                        isAllowed = true;
                        break;
                    }
                }
                if (pipe.entryType == Enums.PipeEntryType.Blacklist) isAllowed = !isAllowed;
                if (!isAllowed)
                    continue;
            }

            //Enter pipe
            pipeEntering = pipe;
            pipeDirection = Vector2.down;

            ResetStuff();
            body.velocity = Vector2.down;
            transform.position = body.position = new Vector2(obj.transform.position.x, transform.position.y);

            photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Sound_Powerdown);
            break;
        }
    }

    void ResetStuff()
    {
        body.velocity = Vector2.zero;
        crouching = false;
        sliding = false;
        propeller = false;
        drill = false;
        usedPropellerThisJump = false;
        groundpound = false;
    }

    void UpwardsPipeCheck() {
        if (!photonView.IsMine || groundpound || !hitRoof || joystick.y < analogDeadzone || state == Enums.PowerupState.MegaMushroom)
            return;

        //todo: change to nonalloc?
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(body.position, Vector2.up, 1f)) {
            GameObject obj = hit.transform.gameObject;
            if (!obj.CompareTag("pipe"))
                continue;
            PipeManager pipe = obj.GetComponent<PipeManager>();
            if (pipe.miniOnly && state != Enums.PowerupState.MiniMushroom)
                continue;
            if (!pipe.entryAllowed)
                continue;
            if (pipe.entryType != Enums.PipeEntryType.AllowAll)
            {
                if (pipe.entryType == Enums.PipeEntryType.PowerupOnly && pipe.powerupList != null)
                {
                    bool allowed = false;
                    foreach(Enums.PowerupState state in pipe.powerupList)
                    {
                        if (state == this.state)
                        {
                            allowed = true;
                            break;
                        }
                    }
                    if (!allowed)
                    {
                        continue;
                    }
                } else if (pipe.playerList != null)
                {
                    bool isAllowed = false;
                    foreach (string p in pipe.playerList)
                    {
                        if (p.ToLower() == photonView.Owner.NickName.ToLower())
                        {
                            isAllowed = true;
                            break;
                        }
                    }
                    if (pipe.entryType == Enums.PipeEntryType.Blacklist) isAllowed = !isAllowed;
                    if (!isAllowed)
                        continue;
                }

            }
            //pipe found
            pipeEntering = pipe;
            pipeDirection = Vector2.up;

            body.velocity = Vector2.up;
            transform.position = body.position = new Vector2(obj.transform.position.x, transform.position.y);

            photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Sound_Powerdown);
            crouching = false;
            sliding = false;
            propeller = false;
            usedPropellerThisJump = false;
            flying = false;
            break;
        }
    }
#endregion

    void HandleCrouching(bool crouchInput) {
        if (!photonView.IsMine || sliding || propeller || knockback)
            return;

        if (state == Enums.PowerupState.MegaMushroom) {
            crouching = false;
            return;
        }
        bool prevCrouchState = crouching || groundpound;
        crouching = ((onGround && crouchInput && !groundpound) || (!onGround && crouchInput && crouching) || (crouching && ForceCrouchCheck())) && !holding;
        if (crouching && !prevCrouchState) {
            //crouch start sound
            photonView.RPC("PlaySound", RpcTarget.All, state == Enums.PowerupState.BlueShell ? Enums.Sounds.Powerup_BlueShell_Enter : Enums.Sounds.Player_Sound_Crouch);
        }
    }

    bool ForceCrouchCheck() {
        //janky fortress ceilingn check, m8
        if (state == Enums.PowerupState.BlueShell && onGround && SceneManager.GetActiveScene().buildIndex != 4)
            return false;
        if (state <= Enums.PowerupState.MiniMushroom)
            return false;

        float width = MainHitbox.bounds.extents.x;

        bool triggerState = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = false;

        float uncrouchHeight = GetHitboxSize(false).y * transform.lossyScale.y;

        bool ret = Physics2D.BoxCast(body.position + Vector2.up * 0.1f, new(width - 0.05f, 0.05f), 0, Vector2.up, uncrouchHeight - 0.1f, Layers.MaskOnlyGround);

        Physics2D.queriesHitTriggers = triggerState;
        return ret;
    }

    void HandleWallslide(bool holdingLeft, bool holdingRight, bool jump) {

        Vector2 currentWallDirection;
        if (holdingLeft) {
            currentWallDirection = Vector2.left;
        } else if (holdingRight) {
            currentWallDirection = Vector2.right;
        } else if (wallSlideLeft) {
            currentWallDirection = Vector2.left;
        } else if (wallSlideRight) {
            currentWallDirection = Vector2.right;
        } else {
            return;
        }

        HandleWallSlideChecks(currentWallDirection, holdingRight, holdingLeft);

        wallSlideRight &= wallSlideTimer > 0 && hitRight;
        wallSlideLeft &= wallSlideTimer > 0 && hitLeft;

        if (wallSlideLeft || wallSlideRight) {
            //walljump check
            facingRight = wallSlideLeft;
            if (jump && wallJumpTimer <= 0) {
                //perform walljump

                hitRight = false;
                hitLeft = false;
                body.velocity = new Vector2(runningMaxSpeed * (3 / 4f) * (wallSlideLeft ? 1 : -1), walljumpVelocity);
                singlejump = false;
                doublejump = false;
                triplejump = false;
                onGround = false;
                bounce = false;
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Sound_WallJump);
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Voice_WallJump, (byte) Random.Range(1, 3));

                Vector2 offset = new(MainHitbox.size.x / 2f * (wallSlideLeft ? -1 : 1), MainHitbox.size.y / 2f);
                photonView.RPC("SpawnParticle", RpcTarget.All, "Prefabs/Particle/WalljumpParticle", body.position + offset, wallSlideLeft ? Vector3.zero : Vector3.up * 180);

                wallJumpTimer = 16 / 60f;
                animator.SetTrigger("walljump");
                wallSlideTimer = 0;
            }
        } else {
            //walljump starting check
            bool canWallslide = !inShell && body.velocity.y < -0.1 && !groundpound && !onGround && !holding && state != Enums.PowerupState.MegaMushroom && !flying && !drill && !crouching && !sliding && !knockback;
            if (!canWallslide)
                return;

            //Check 1
            if (wallJumpTimer > 0)
                return;

            //Check 2
            if (wallSlideTimer - Time.fixedDeltaTime <= 0)
                return;

            //Check 4: already handled
            //Check 5.2: already handled

            //Check 6
            if (crouching)
                return;

            //Check 8
            if (!((currentWallDirection == Vector2.right && facingRight) || (currentWallDirection == Vector2.left && !facingRight)))
                return;

            //Start wallslide
            wallSlideRight = currentWallDirection == Vector2.right;
            wallSlideLeft = currentWallDirection == Vector2.left;
            propeller = false;
        }

        wallSlideRight &= wallSlideTimer > 0 && hitRight;
        wallSlideLeft &= wallSlideTimer > 0 && hitLeft;
    }

    void HandleWallSlideChecks(Vector2 wallDirection, bool right, bool left) {
        bool floorCheck = !Physics2D.Raycast(body.position, Vector2.down, 0.3f, Layers.MaskAnyGround);
        if (!floorCheck) {
            wallSlideTimer = 0;
            return;
        }

        bool moveDownCheck = body.velocity.y < 0;
        if (!moveDownCheck)
            return;

        bool wallCollisionCheck = wallDirection == Vector2.left ? hitLeft : hitRight;
        if (!wallCollisionCheck)
            return;

        bool heightLowerCheck = Physics2D.Raycast(body.position + new Vector2(0, .2f), wallDirection, MainHitbox.size.x * 2, Layers.MaskOnlyGround);
        if (!heightLowerCheck)
            return;

        if ((wallDirection == Vector2.left && !left) || (wallDirection == Vector2.right && !right))
            return;

        wallSlideTimer = 16 / 60f;
    }

    void HandleJumping(bool jump) {
        if (knockback || drill || (state == Enums.PowerupState.MegaMushroom && singlejump))
            return;

        bool topSpeed = Mathf.Abs(body.velocity.x) + 0.5f > (runningMaxSpeed * (invincible > 0 ? 1.5F : 1));
        if (bounce || (jump && (onGround || (koyoteTime < 0.07f && !propeller)) && !startedSliding)) {

            bool canSpecialJump = (jump || (bounce && jumpHeld)) && properJump && !flying && !propeller && topSpeed && landing < 0.45f && !holding && !triplejump && !crouching && !inShell && invincible <= 0 && ((body.velocity.x < 0 && !facingRight) || (body.velocity.x > 0 && facingRight)) && !Physics2D.Raycast(body.position + new Vector2(0, 0.1f), Vector2.up, 1f, Layers.MaskOnlyGround);
            float jumpBoost = 0;

            koyoteTime = 1;
            jumpBuffer = 0;
            skidding = false;
            turnaround = false;
            sliding = false;
            wallSlideLeft = false;
            wallSlideRight = false;
            //alreadyGroundpounded = false;
            groundpound = false;
            groundpoundCounter = 0;
            drill = false;
            flying &= bounce;
            propeller &= bounce;

            if (!bounce && onSpinner && !holding) {
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Voice_SpinnerLaunch);
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.World_Spinner_Launch);
                body.velocity = new Vector2(body.velocity.x, launchVelocity);
                flying = true;
                onGround = false;
                body.position += Vector2.up * 0.075f;
                doGroundSnap = false;
                previousOnGround = false;
                crouching = false;
                inShell = false;
                return;
            }

            float vel = state switch {
                Enums.PowerupState.MegaMushroom => megaJumpVelocity,
                _ => jumpVelocity + Mathf.Abs(body.velocity.x) / runningMaxSpeed * 1.05f,
            };


            if (canSpecialJump && singlejump) {
                //Double jump
                singlejump = false;
                doublejump = true;
                triplejump = false;
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Voice_DoubleJump, (byte) Random.Range(1, 3));
            } else if (canSpecialJump && doublejump) {
                //Triple Jump
                singlejump = false;
                doublejump = false;
                triplejump = true;
                jumpBoost = 0.5f;
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Voice_TripleJump);
            } else {
                //Normal jump
                singlejump = true;
                doublejump = false;
                triplejump = false;
            }
            body.velocity = new Vector2(body.velocity.x, vel + jumpBoost);
            onGround = false;
            doGroundSnap = false;
            body.position += Vector2.up * 0.075f;
            groundpoundCounter = 0;
            properJump = true;
            jumping = true;

            if (!bounce) {
                //play jump sound
                Enums.Sounds sound = state switch {
                    Enums.PowerupState.MiniMushroom => Enums.Sounds.Powerup_MiniMushroom_Jump,
                    Enums.PowerupState.MegaMushroom => Enums.Sounds.Powerup_MegaMushroom_Jump,
                    _ => Enums.Sounds.Player_Sound_Jump,
                };
                photonView.RPC("PlaySound", RpcTarget.All, sound);
            }
            bounce = false;
        }
    }


    public void UpdateHitbox() {
        bool crouchHitbox = state != Enums.PowerupState.MiniMushroom && pipeEntering == null && ((crouching && !groundpound) || inShell || sliding);
        Vector2 hitbox = GetHitboxSize(crouchHitbox);

        MainHitbox.size = hitbox;
        MainHitbox.offset = Vector2.up * 0.5f * hitbox;
    }

    public Vector2 GetHitboxSize(bool crouching) {
        float height;

        if (state <= Enums.PowerupState.Small || (invincible > 0 && !onGround && !crouching && !sliding && !flying && !propeller) || groundpound) {
            height = heightSmallModel;
        } else {
            height = heightLargeModel;
        }

        if (crouching)
            height *= state <= Enums.PowerupState.Small ? 0.7f : 0.5f;

        return new(MainHitbox.size.x, height);
    }

    void HandleWalkingRunning(bool left, bool right) {
        if (groundpound || groundpoundCounter > 0 || sliding || knockback || pipeEntering || jumpLandingTimer > 0 || !(wallJumpTimer <= 0 || onGround || body.velocity.y < 0))
            return;

        iceSliding = false;
        if (!left && !right) {
            skidding = false;
            turnaround = false;
            if (doIceSkidding)
                iceSliding = true;
        }

        if (Mathf.Abs(body.velocity.x) < 0.5f || !onGround)
            skidding = false;

        if (inShell) {
            body.velocity = new(runningMaxSpeed * 0.9f * (facingRight ? 1 : -1) * (1f - slowdownTimer), body.velocity.y);
            return;
        }

        if (!(left ^ right))
            return;

        if (crouching && state == Enums.PowerupState.BlueShell)
            return;

        float airPenalty = onGround ? 1 : 0.5f;
        float xVel = Mathf.Abs(body.velocity.x);
        float invincibleSpeedBoost = onGround && invincible > 0 ? 2f : 1f;
        invincibleSpeedBoost *= state == Enums.PowerupState.Weed ? 1.75f : 1f;
        float runSpeedTotal = runningMaxSpeed * invincibleSpeedBoost;
        float walkSpeedTotal = walkingMaxSpeed;
        bool reverseDirection = (left ? 1 : -1) == Mathf.Sign(body.velocity.x); // ((left && body.velocity.x > 0.02) || (right && body.velocity.x < -0.02));
        float reverseFloat = reverseDirection && doIceSkidding ? 0.4f : 1;
        float turnaroundSpeedBoost = turnaround && !reverseDirection ? 5 : 1;
        float stationarySpeedBoost = Mathf.Abs(body.velocity.x) <= 0.005f ? 1f : 1f;
        float propellerBoost = propellerTimer > 0 ? 2.5f : 1;
        float drillSlowing = drill ? 0.25f : 1f;

        bool run = functionallyRunning && !flying;

        if ((crouching && !onGround) || !crouching) {
            if (run && xVel >= walkSpeedTotal && !reverseDirection) {
                //running
                skidding = false;
                turnaround = false;
                float change = propellerBoost * invincibleSpeedBoost * turnaroundSpeedBoost * runningAcceleration * airPenalty * stationarySpeedBoost * drillSlowing * Time.fixedDeltaTime;
                if (invincibleSpeedBoost > 1 && xVel > runningMaxSpeed)
                    change *= 5;

                change *= left ? -1 : 1;
                body.velocity = new(Mathf.Clamp(body.velocity.x + change, -runSpeedTotal, runSpeedTotal), body.velocity.y);

            } else {
                //walking
                float change = propellerBoost * invincibleSpeedBoost * reverseFloat * turnaroundSpeedBoost * walkingAcceleration * stationarySpeedBoost * drillSlowing * Time.fixedDeltaTime;
                change *= left ? -1 : 1;
                if (xVel <= walkSpeedTotal || reverseDirection) {
                    body.velocity += Vector2.right * change;
                    if (!reverseDirection)
                        body.velocity = new(Mathf.Clamp(body.velocity.x, -walkSpeedTotal, walkSpeedTotal), body.velocity.y);

                    if (xVel == walkSpeedTotal) {
                        skidding = false;
                        turnaround = false;
                    }
                }

                if (state != Enums.PowerupState.MegaMushroom && reverseDirection && xVel >= runningMaxSpeed - 2 && onGround) {
                    skidding = true;
                    turnaround = true;
                    facingRight = left;
                }
            }
        } else {
            turnaround = false;
            skidding = false;
        }

        inShell |= state == Enums.PowerupState.BlueShell && onGround && !inShell && functionallyRunning && !holding && Mathf.Abs(xVel) + 0.25f >= runningMaxSpeed && landing > 0.15f;
        if (onGround || previousOnGround)
            body.velocity = new(body.velocity.x, 0);
    }

    bool HandleStuckInBlock() {
        if (!body || state == Enums.PowerupState.MegaMushroom)
            return false;

        Vector2 checkSize = WorldHitboxSize * new Vector2(1, 0.75f);
        Vector2 checkPos = transform.position + (Vector3) (Vector2.up * checkSize / 2f);

        if (!Utils.IsAnyTileSolidBetweenWorldBox(checkPos, checkSize * 0.9f, false)) {
            alreadyStuckInBlock = stuckInBlock = false;
            return false;
        }
        stuckInBlock = true;
        body.gravityScale = 0;
        body.velocity = Vector2.zero;
        groundpound = false;
        propeller = false;
        drill = false;
        flying = false;
        onGround = true;

        if (!alreadyStuckInBlock) {
            // Code for mario to instantly teleport to the closest free position when he gets stuck

            //prevent mario from clipping to the floor if we got pushed in via our hitbox changing (shell on ice, for example)
            transform.position = body.position = previousFramePosition;
            checkPos = transform.position + (Vector3) (Vector2.up * checkSize / 2f);

            float distanceInterval = 0.025f;
            float minimDistance = 0.95f; // if the minimum actual distance is anything above this value this code will have no effect
            float travelDistance = 0;
            float targetInd = -1; // Basically represents the index of the interval that'll be chosen for mario to be popped out
            int angleInterval = 45;

            for (float i = 0; i < 360 / angleInterval; i ++) { // Test for every angle in the given interval
                float ang = i * angleInterval;
                float testDistance = 0;

                float radAngle = Mathf.PI * ang / 180;
                Vector2 testPos;

                // Calculate the distance mario would have to be moved on a certain angle to stop collisioning
                do {
                    testPos = checkPos + new Vector2(Mathf.Cos(radAngle) * testDistance, Mathf.Sin(radAngle) * testDistance);
                    testDistance += distanceInterval;
                }
                while (Utils.IsAnyTileSolidBetweenWorldBox(testPos, checkSize * 0.975f));

                // This is to give right angles more priority over others when deciding
                float adjustedDistance = testDistance * (1 + Mathf.Abs(Mathf.Sin(radAngle * 2) / 2));

                // Set the new minimum only if the new position is inside of the visible level
                if (testPos.y > GameManager.Instance.cameraMinY && testPos.x > GameManager.Instance.cameraMinX && testPos.x < GameManager.Instance.cameraMaxX){
                    if (adjustedDistance < minimDistance) {
                        minimDistance = adjustedDistance;
                        travelDistance = testDistance;
                        targetInd = i;
                    }
                }
            }

            // Move him
            if (targetInd != -1) {
                float radAngle = Mathf.PI * (targetInd * angleInterval) / 180;
                Vector2 lastPos = checkPos;
                checkPos += new Vector2(Mathf.Cos(radAngle) * travelDistance, Mathf.Sin(radAngle) * travelDistance);
                transform.position = body.position = new(checkPos.x, body.position.y + (checkPos.y - lastPos.y));
                stuckInBlock = false;
                return false; // Freed
            }
        }

        alreadyStuckInBlock = true;
        body.velocity = Vector2.right * 2f;
        return true;
    }

    void TickCounters() {
        float delta = Time.fixedDeltaTime;
        if (!pipeEntering)
            Utils.TickTimer(ref invincible, 0, delta);

        Utils.TickTimer(ref throwInvincibility, 0, delta);
        Utils.TickTimer(ref jumpBuffer, 0, delta);
        if (giantStartTimer <= 0)
            Utils.TickTimer(ref giantTimer, 0, delta);
        Utils.TickTimer(ref giantStartTimer, 0, delta);
        Utils.TickTimer(ref groundpoundCounter, 0, delta);
        Utils.TickTimer(ref giantEndTimer, 0, delta);
        Utils.TickTimer(ref groundpoundDelay, 0, delta);
        Utils.TickTimer(ref hitInvincibilityCounter, 0, delta);
        Utils.TickTimer(ref propellerSpinTimer, 0, delta);
        Utils.TickTimer(ref propellerTimer, 0, delta);
        Utils.TickTimer(ref knockbackTimer, 0, delta);
        Utils.TickTimer(ref pipeTimer, 0, delta);
        Utils.TickTimer(ref wallSlideTimer, 0, delta);
        Utils.TickTimer(ref wallJumpTimer, 0, delta);
        Utils.TickTimer(ref jumpLandingTimer, 0, delta);
        Utils.TickTimer(ref pickupTimer, 0, -delta, pickupTime);
        Utils.TickTimer(ref fireballTimer, 0, delta);
        Utils.TickTimer(ref slowdownTimer, 0, delta * 0.5f);

        if (onGround)
            Utils.TickTimer(ref landing, 0, -delta);
    }

    [PunRPC]
    public void FinishMegaMario(bool success) {
        if (success) {
            PlaySoundEverywhere(Enums.Sounds.Player_Voice_MegaMushroom);
        } else {
            //hit a ceiling, cancel
            giantSavedVelocity = Vector2.zero;
            state = Enums.PowerupState.Mushroom;
            giantEndTimer = giantStartTime - giantStartTimer;
            animator.enabled = true;
            animator.Play("mega-cancel", 0, 1f - (giantEndTimer / giantStartTime));
            giantStartTimer = 0;
            stationaryGiantEnd = true;
            storedPowerup = (Powerup) Resources.Load("Scriptables/Powerups/MegaMushroom");
            giantTimer = 0;
            PlaySound(Enums.Sounds.Player_Sound_PowerupReserveStore);
        }
        body.isKinematic = false;
        UpdateGameState();
    }

    void HandleFacingDirection() {
        if (groundpound)
            return;

        //Facing direction
        bool right = joystick.x > analogDeadzone;
        bool left = joystick.x < -analogDeadzone;

        if (wallJumpTimer > 0) {
            facingRight = body.velocity.x > 0;
        } else if (doIceSkidding && !inShell && !sliding) {
            if (right || left)
                facingRight = right;
        } else if (giantStartTimer <= 0 && giantEndTimer <= 0 && !skidding && !(animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround") || turnaround)) {
            if (knockback || (onGround && state != Enums.PowerupState.MegaMushroom && Mathf.Abs(body.velocity.x) > 0.05f)) {
                facingRight = body.velocity.x > 0;
            } else if (((wallJumpTimer <= 0 && !inShell) || giantStartTimer > 0) && (right || left)) {
                facingRight = right;
            }
            if (!inShell && ((Mathf.Abs(body.velocity.x) < 0.5f && crouching) || doIceSkidding) && (right || left))
                facingRight = right;
        }
    }

    [PunRPC]
    public void EndMega() {
        giantEndTimer = giantStartTime / 2f;
        state = Enums.PowerupState.Mushroom;
        stationaryGiantEnd = false;
        hitInvincibilityCounter = 3f;
        PlaySoundEverywhere(Enums.Sounds.Powerup_MegaMushroom_End);
        body.velocity = new(body.velocity.x, body.velocity.y > 0 ? (body.velocity.y / 3f) : body.velocity.y);
    }

    public void HandleBlockSnapping() {
        //if we're about to be in the top 2 pixels of a block, snap up to it, (if we can fit)

        if (body.velocity.y > 0)
            return;

        Vector2 nextPos = body.position + Time.fixedDeltaTime * 2f * body.velocity;

        if (!Utils.IsAnyTileSolidBetweenWorldBox(nextPos + WorldHitboxSize.y * 0.5f * Vector2.up, WorldHitboxSize))
            //we are not going to be inside a block next fixed update
            return;

        //we ARE inside a block. figure out the height of the contact
        // 32 pixels per unit
        bool orig = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = true;
        RaycastHit2D contact = Physics2D.BoxCast(nextPos + 3f / 32f * Vector2.up, new(WorldHitboxSize.y, 1f / 32f), 0, Vector2.down, 3f / 32f, Layers.MaskAnyGround);
        Physics2D.queriesStartInColliders = orig;

        if (!contact || contact.normal.y < 0.1f) {
            //we didn't hit the ground, we must've hit a ceiling or something.
            return;
        }

        float point = contact.point.y + Physics2D.defaultContactOffset;
        if (body.position.y > point + Physics2D.defaultContactOffset) {
            //dont snap when we're above the block
            return;
        }

        Vector2 newPosition = new(body.position.x, point);

        if (Utils.IsAnyTileSolidBetweenWorldBox(newPosition + WorldHitboxSize.y * 0.5f * Vector2.up, WorldHitboxSize)) {
            //it's an invalid position anyway, we'd be inside something.
            return;
        }

        //valid position, snap upwards
        body.position = newPosition;
    }

    void HandleMovement(float delta) {
        functionallyRunning = running || state == Enums.PowerupState.MegaMushroom || propeller;

        if (dead || !spawned)
            return;

        if (photonView.IsMine && body.position.y + transform.lossyScale.y < GameManager.Instance.GetLevelMinY()) {
            //death via pit
            photonView.RPC("Death", RpcTarget.All, true, false);
            return;
        }

        if (Frozen) {
            if (!frozenObject) {
                Unfreeze((byte) IFreezableEntity.UnfreezeReason.Other);
            } else {
                body.velocity = Vector2.zero;
                return;
            }
        }

        if (photonView.IsMine && holding && (holding.dead || Frozen || holding.Frozen))
            photonView.RPC("SetHolding", RpcTarget.All, -1);

        FrozenCube holdingCube;
        if (((holdingCube = holding as FrozenCube) && holdingCube) || ((holdingCube = holdingOld as FrozenCube) && holdingCube)) {
            foreach (BoxCollider2D hitbox in hitboxes) {
                Physics2D.IgnoreCollision(hitbox, holdingCube.hitbox, throwInvincibility > 0);
            }
        }

        bool paused = GameManager.Instance.paused && photonView.IsMine;

        if (giantStartTimer > 0) {
            body.velocity = Vector2.zero;
            transform.position = body.position = previousFramePosition;
            if (giantStartTimer - delta <= 0 && photonView.IsMine) {
                photonView.RPC("FinishMegaMario", RpcTarget.All, true);
                giantStartTimer = 0;
            } else {
                body.isKinematic = true;
                if (animator.GetCurrentAnimatorClipInfo(0).Length <= 0 || animator.GetCurrentAnimatorClipInfo(0)[0].clip.name != "mega-scale")
                    animator.Play("mega-scale");


                Vector2 checkSize = WorldHitboxSize * new Vector2(0.75f, 1.1f);
                Vector2 normalizedVelocity = body.velocity;
                if (!groundpound)
                    normalizedVelocity.y = Mathf.Max(0, body.velocity.y);

                Vector2 offset = Vector2.zero;
                if (singlejump && onGround)
                    offset = Vector2.down / 2f;

                Vector2 checkPosition = body.position + Vector2.up * checkSize / 2f + offset;

                Vector3Int minPos = Utils.WorldToTilemapPosition(checkPosition - (checkSize / 2), wrap: false);
                Vector3Int size = Utils.WorldToTilemapPosition(checkPosition + (checkSize / 2), wrap: false) - minPos;

                for (int x = 0; x <= size.x; x++) {
                    Vector3Int tileLocation = new(minPos.x + x, minPos.y + size.y, 0);
                    Utils.WrapTileLocation(ref tileLocation);
                    TileBase tile = Utils.GetTileAtTileLocation(tileLocation);

                    bool cancelMega;
                    if (tile is BreakableBrickTile bbt)
                        cancelMega = !bbt.breakableByGiantMario;
                    else
                        cancelMega = Utils.IsTileSolidAtTileLocation(tileLocation);

                    if (cancelMega) {
                        photonView.RPC("FinishMegaMario", RpcTarget.All, false);
                        return;
                    }
                }
            }
            return;
        }
        if (giantEndTimer > 0 && stationaryGiantEnd) {
            body.velocity = Vector2.zero;
            body.isKinematic = true;
            transform.position = body.position = previousFramePosition;

            if (giantEndTimer - delta <= 0) {
                hitInvincibilityCounter = 2f;
                body.velocity = giantSavedVelocity;
                animator.enabled = true;
                body.isKinematic = false;
                state = previousState;
                UpdateGameState();
            }
            return;
        }

        if (state == Enums.PowerupState.MegaMushroom) {
            HandleGiantTiles(true);
            if (onGround && singlejump) {
                photonView.RPC("SpawnParticle", RpcTarget.All, "Prefabs/Particle/GroundpoundDust", body.position);
                CameraController.ScreenShake = 0.15f;
                singlejump = false;
            }
            invincible = 0;
        }

        //pipes > stuck in block, else the animation gets janked.
        if (doorEntering || pipeEntering || giantStartTimer > 0 || (giantEndTimer > 0 && stationaryGiantEnd) || animator.GetBool("pipe"))
            return;
        if (HandleStuckInBlock())
            return;

        //Pipes & Doors
        if (!paused) {
            if (pipeTimer <= 0)
            {
                DownwardsPipeCheck();
                UpwardsPipeCheck();
            }
            if (!doorEntering)
            {
                DoorCheck();
            }
        }

        if (knockback) {
            if (bounce && photonView.IsMine)
                photonView.RPC("ResetKnockback", RpcTarget.All);

            wallSlideLeft = false;
            wallSlideRight = false;
            crouching = false;
            inShell = false;
            body.velocity -= body.velocity * (delta * 2f);
            if (photonView.IsMine && onGround && Mathf.Abs(body.velocity.x) < 0.2f && knockbackTimer <= 0)
                photonView.RPC("ResetKnockback", RpcTarget.All);
            if (holding) {
                holding.photonView.RPC("Throw", RpcTarget.All, !facingRight, true);
                holding = null;
            }
        }

        //activate blocks jumped into
        if (hitRoof) {
            body.velocity = new Vector2(body.velocity.x, Mathf.Min(body.velocity.y, -0.1f));
            bool tempHitBlock = false;
            foreach (Vector3Int tile in tilesJumpedInto) {
                int temp = InteractWithTile(tile, InteractableTile.InteractionDirection.Up);
                if (temp != -1)
                    tempHitBlock |= temp == 1;
            }
            if (tempHitBlock && state == Enums.PowerupState.MegaMushroom) {
                CameraController.ScreenShake = 0.15f;
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.World_Block_Bump);
            }
        }

        bool right = joystick.x > analogDeadzone && !paused;
        bool left = joystick.x < -analogDeadzone && !paused;
        bool crouch = joystick.y < -analogDeadzone && !paused;
        alreadyGroundpounded &= crouch;
        bool up = joystick.y > analogDeadzone && !paused;
        bool jump = jumpBuffer > 0 && (onGround || koyoteTime < 0.07f || wallSlideLeft || wallSlideRight) && !paused;


        if (state == Enums.PowerupState.OppressorMKII)
        {
            HandleOppressorPhysics(left, right, crouch, up, jump);
            return;
        }

        if (drill) {
            propellerSpinTimer = 0;
            if (propeller) {
                if (!crouch) {
                    Utils.TickTimer(ref propellerDrillBuffer, 0, Time.deltaTime);
                    if (propellerDrillBuffer <= 0)
                        drill = false;
                } else {
                    propellerDrillBuffer = 0.15f;
                }
            }
        }

        if (propellerTimer > 0)
            body.velocity = new Vector2(body.velocity.x, propellerLaunchVelocity - (propellerTimer < .4f ? (1 - (propellerTimer / .4f)) * propellerLaunchVelocity : 0));

        if (powerupButtonHeld && wallJumpTimer <= 0 && (propeller || !usedPropellerThisJump)) {
            if (body.velocity.y < -0.1f && propeller && !drill && !wallSlideLeft && !wallSlideRight && propellerSpinTimer < propellerSpinTime / 4f) {
                propellerSpinTimer = propellerSpinTime;
                propeller = true;
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Powerup_PropellerMushroom_Spin);
            }
        }

        if (holding) {
            wallSlideLeft = false;
            wallSlideRight = false;
            SetHoldingOffset();
        }

        //throwing held item
        ThrowHeldItem(left, right, crouch);

        //blue shell enter/exit
        if (state != Enums.PowerupState.BlueShell || !functionallyRunning)
            inShell = false;

        if (inShell) {
            crouch = true;
            if (photonView.IsMine && (hitLeft || hitRight)) {
                foreach (var tile in tilesHitSide)
                    InteractWithTile(tile, InteractableTile.InteractionDirection.Up);
                facingRight = hitLeft;
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.World_Block_Bump);
            }
        }

        //Ground
        if (onGround) {
            if (photonView.IsMine && hitRoof && crushGround && body.velocity.y <= 0.1 && state != Enums.PowerupState.MegaMushroom) {
                //Crushed.
                photonView.RPC("Powerdown", RpcTarget.All, true);
            }

            koyoteTime = 0;
            usedPropellerThisJump = false;
            wallSlideLeft = false;
            wallSlideRight = false;
            jumping = false;
            if (drill)
                SpawnParticle("Prefabs/Particle/GroundpoundDust", body.position);

            if (onSpinner && Mathf.Abs(body.velocity.x) < 0.3f && !holding) {
                Transform spnr = onSpinner.transform;
                float diff = body.position.x - spnr.transform.position.x;
                if (Mathf.Abs(diff) >= 0.02f)
                    body.position += -0.6f * Mathf.Sign(diff) * Time.fixedDeltaTime * Vector2.right;
            }
        } else {
            koyoteTime += delta;
            landing = 0;
            skidding = false;
            turnaround = false;
            if (!jumping)
                properJump = false;
        }

        //Crouching
        HandleCrouching(crouch);

        HandleWallslide(left, right, jump);

        HandleSlopes();

        if (crouch && !alreadyGroundpounded) {
            HandleGroundpoundStart(left, right);
        } else {
            groundpoundStartTimer = 0;
        }
        HandleGroundpound();

        if (onGround) {
            if (propellerTimer < 0.5f) {
                propeller = false;
                propellerTimer = 0;
            }
            flying = false;
            drill = false;
            if (landing <= Time.fixedDeltaTime + 0.01f && !groundpound && !crouching && !inShell && !holding && state != Enums.PowerupState.MegaMushroom) {
                bool edge = !Physics2D.BoxCast(body.position, MainHitbox.size * 0.75f, 0, Vector2.down, 0, Layers.MaskAnyGround);
                bool edgeLanding = false;
                if (edge) {
                    bool rightEdge = edge && Utils.IsTileSolidAtWorldLocation(body.position + new Vector2(0.25f, -0.25f));
                    bool leftEdge = edge && Utils.IsTileSolidAtWorldLocation(body.position + new Vector2(-0.25f, -0.25f));
                    edgeLanding = (leftEdge || rightEdge) && properJump && edge && (facingRight == rightEdge);
                }

                if ((triplejump && !(left ^ right))
                    || edgeLanding
                    || (Mathf.Abs(body.velocity.x) < 0.1f)) {

                    if (!doIceSkidding)
                        body.velocity = Vector2.zero;

                    animator.Play("jumplanding" + (edgeLanding ? "-edge" : ""));
                    if (edgeLanding)
                        jumpLandingTimer = 0.15f;
                }
            }
            if (landing > 0.2f) {
                singlejump = false;
                doublejump = false;
                triplejump = false;
            }
        }


        if ((wallJumpTimer <= 0 || onGround) && !(groundpound && !onGround)) {
            //Normal walking/running
            HandleWalkingRunning(left, right);

            //Jumping
            HandleJumping(jump);
        }


        if (state == Enums.PowerupState.MegaMushroom && giantTimer <= 0 && photonView.IsMine) {
            photonView.RPC("EndMega", RpcTarget.All);
        }

        HandleSlopes();
        HandleSliding(up, crouch);
        HandleFacingDirection();

        //slow-rise check
        if (flying || propeller) {
            body.gravityScale = flyingGravity;
        } else {
            float gravityModifier = state switch {
                Enums.PowerupState.MiniMushroom => 0.4f,
                _ => 1,
            };
            float slowriseModifier = state switch {
                Enums.PowerupState.MegaMushroom => 3f,
                _ => 1f,
            };
            if (groundpound)
                gravityModifier *= 1.5f;

            if (body.velocity.y > 2.5) {
                if (jump || jumpHeld || state == Enums.PowerupState.MegaMushroom) {
                    body.gravityScale = slowriseGravity * slowriseModifier;
                } else {
                    body.gravityScale = normalGravity * 1.5f * gravityModifier;
                }
            } else if (onGround || (groundpound && groundpoundCounter > 0)) {
                body.gravityScale = 0f;
            } else {
                body.gravityScale = normalGravity * (gravityModifier / 1.2f);
            }
        }

        bool uphill = sliding && (floorAngle != 0 && Mathf.Sign(floorAngle) == Mathf.Sign(body.velocity.x));
        if (!inShell && !(sliding && Mathf.Abs(floorAngle) > slopeSlidingAngle * 2 && !uphill)) {
            bool abovemax = false;
            float invincibleSpeedBoost = invincible > 0 ? 1.5f : 1;
            invincibleSpeedBoost *= state == Enums.PowerupState.Weed ? 2f : 1f;
            float uphillChange = uphill ? (1 - (Mathf.Abs(floorAngle) / 360f)) : 1;
            float max;
            if (onGround) {
                max = functionallyRunning ? runningMaxSpeed * invincibleSpeedBoost : walkingMaxSpeed;
                max *= uphillChange;
            } else {
                max = runningMaxSpeed;
            }

            if (knockback) {
                abovemax = true;
            } else if (flying || propeller) {
                abovemax = !(left || right);
            } else if (!sliding && (left ^ right) && !crouching) {
                abovemax = Mathf.Abs(body.velocity.x) > max;
            } else if ((left ^ right) && Mathf.Abs(floorAngle) > slopeSlidingAngle * 2) {
                abovemax = Mathf.Abs(body.velocity.x) > (Mathf.Abs(floorAngle) / 30f);
            } else if (onGround) {
                abovemax = true;
            }
            //Friction...
            if (abovemax) {
                float multiplier = 1 - (delta * tileFriction * (knockback ? 1f : 4f) * (sliding ? 0.7f : 1f) * (crouching ? 0.5f : 1f ) * uphillChange);
                body.velocity = new(body.velocity.x * multiplier, body.velocity.y);
                if (Mathf.Abs(body.velocity.x) < 0.15f)
                    body.velocity = new Vector2(0, body.velocity.y);
            }
        }
        //Terminal velocity
        float terminalVelocityModifier = state switch {
            Enums.PowerupState.MiniMushroom => 0.625f,
            Enums.PowerupState.MegaMushroom => 2f,
            _ => 1f,
        };
        if (flying) {
            if (drill) {
                body.velocity = new(Mathf.Clamp(body.velocity.x, -walkingMaxSpeed, walkingMaxSpeed), -drillVelocity);
            } else {
                body.velocity = new(Mathf.Clamp(body.velocity.x, -walkingMaxSpeed, walkingMaxSpeed), Mathf.Max(body.velocity.y, -flyingTerminalVelocity));
            }
        } else if (propeller) {
            if (drill) {
                body.velocity = new(Mathf.Clamp(body.velocity.x, -walkingMaxSpeed, walkingMaxSpeed), -drillVelocity);
            } else {
                float htv = walkingMaxSpeed * 1.18f + (propellerTimer * 2f);
                body.velocity = new(Mathf.Clamp(body.velocity.x, -htv, htv), Mathf.Max(body.velocity.y, propellerSpinTimer > 0 ? -propellerSpinFallSpeed : -propellerFallSpeed));
            }
        } else if (wallSlideLeft || wallSlideRight) {
            body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, wallslideSpeed));
        } else if (groundpound) {
            body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, -groundpoundVelocity));
        } else {
            body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, terminalVelocity * terminalVelocityModifier));
        }

        if (crouching || sliding || skidding) {
            wallSlideLeft = false;
            wallSlideRight = false;
        }

        if (previousOnGround && !onGround && !properJump && crouching && !inShell && !groundpound)
            body.velocity = new(body.velocity.x, -3.75f);
    }

    public void SetHoldingOffset() {
        if (holding is FrozenCube) {
            holding.holderOffset = new(0, MainHitbox.size.y * (1f - Utils.QuadraticEaseOut(1f - (pickupTimer / pickupTime))), -2);
        } else {
            holding.holderOffset = new((facingRight ? 1 : -1) * 0.25f, state >= Enums.PowerupState.Mushroom ? 0.5f : 0.25f, !facingRight ? -0.09f : 0f);
        }
    }

    void ThrowHeldItem(bool left, bool right, bool crouch) {
        if (!((!functionallyRunning || state == Enums.PowerupState.MiniMushroom || state == Enums.PowerupState.MegaMushroom || invincible > 0 || flying || propeller) && holding))
            return;

        bool throwLeft = !facingRight;
        if (left ^ right)
            throwLeft = left;

        crouch &= holding.canPlace;

        holdingOld = holding;
        throwInvincibility = 0.15f;

        if (photonView.IsMine)
            holding.photonView.RPC("Throw", RpcTarget.All, throwLeft, crouch);

        if (!crouch && !knockback) {
            PlaySound(Enums.Sounds.Player_Voice_WallJump, 2);
            throwInvincibility = 0.5f;
            animator.SetTrigger("throw");
        }

        holding = null;
    }

    void HandleGroundpoundStart(bool left, bool right) {
        if (!photonView.IsMine)
            return;

        if (groundpoundStartTimer == 0)
            groundpoundStartTimer = 0.065f;

        Utils.TickTimer(ref groundpoundStartTimer, 0, Time.fixedDeltaTime);

        if (groundpoundStartTimer != 0)
            return;

        if (onGround || knockback || groundpound || drill
            || holding || crouching || sliding
            || wallSlideLeft || wallSlideRight || groundpoundDelay > 0)

            return;
        if (!propeller && !flying && (left || right))
            return;

        if (flying) {
            //start drill
            if (body.velocity.y < 0) {
                drill = true;
                hitBlock = true;
                body.velocity = new(0, body.velocity.y);
            }
        } else if (propeller) {
            //start propeller drill
            if (propellerTimer < 0.6f) {
                drill = true;
                propellerTimer = 0;
                hitBlock = true;
            }
        } else {
            //start groundpound
            //check if high enough above ground
            if (Physics2D.BoxCast(body.position, MainHitbox.size * Vector2.right * transform.localScale, 0, Vector2.down, 0.15f * (state == Enums.PowerupState.MegaMushroom ? 2.5f : 1), Layers.MaskAnyGround))
                return;

            wallSlideLeft = false;
            wallSlideRight = false;
            groundpound = true;
            singlejump = false;
            doublejump = false;
            triplejump = false;
            hitBlock = true;
            sliding = false;
            body.velocity = Vector2.up * 1.5f;
            groundpoundCounter = groundpoundTime * (state == Enums.PowerupState.MegaMushroom ? 1.5f : 1);
            photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Player_Sound_GroundpoundStart);
            alreadyGroundpounded = true;
            //groundpoundDelay = 0.75f;
        }
    }

    void HandleGroundpound() {
        if (groundpound && groundpoundCounter > 0 && groundpoundCounter <= .1f)
            body.velocity = Vector2.zero;

        if (groundpound && groundpoundCounter > 0 && groundpoundCounter - Time.fixedDeltaTime <= 0)
            body.velocity = Vector2.down * groundpoundVelocity;

        if (!(photonView.IsMine && onGround && (groundpound || drill) && hitBlock))
            return;

        bool tempHitBlock = false, hitAnyBlock = false;
        foreach (Vector3Int tile in tilesStandingOn) {
            int temp = InteractWithTile(tile, InteractableTile.InteractionDirection.Down);
            if (temp != -1) {
                hitAnyBlock = true;
                tempHitBlock |= temp == 1;
            }
        }
        hitBlock = tempHitBlock;
        if (drill) {
            flying &= hitBlock;
            propeller &= hitBlock;
            drill = hitBlock;
            if (hitBlock)
                onGround = false;
        } else {
            //groundpound
            if (hitAnyBlock) {
                if (state != Enums.PowerupState.MegaMushroom) {
                    Enums.Sounds sound = state switch {
                        Enums.PowerupState.MiniMushroom => Enums.Sounds.Powerup_MiniMushroom_Groundpound,
                        _ => Enums.Sounds.Player_Sound_GroundpoundLanding,
                    };
                    photonView.RPC("PlaySound", RpcTarget.All, sound);
                    photonView.RPC("SpawnParticle", RpcTarget.All, "Prefabs/Particle/GroundpoundDust", body.position);
                    groundpoundDelay = 0;
                } else {
                    CameraController.ScreenShake = 0.15f;
                }
            }
            if (hitBlock) {
                koyoteTime = 1.5f;
            } else if (state == Enums.PowerupState.MegaMushroom) {
                photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Powerup_MegaMushroom_Groundpound);
                photonView.RPC("SpawnParticle", RpcTarget.All, "Prefabs/Particle/GroundpoundDust", body.position);
                CameraController.ScreenShake = 0.35f;
            }
        }
    }

    public bool CanPickup() {
        return state != Enums.PowerupState.MiniMushroom && !skidding && !turnaround && !holding && running && !propeller && !flying && !crouching && !dead && !wallSlideLeft && !wallSlideRight && !doublejump && !triplejump && !groundpound;
    }
    void OnDrawGizmos() {
        if (!body)
            return;

        Gizmos.DrawRay(body.position, body.velocity);
        Gizmos.DrawCube(body.position + new Vector2(0, WorldHitboxSize.y * 0.5f) + (body.velocity * Time.fixedDeltaTime), WorldHitboxSize);

        Gizmos.color = Color.white;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) {
            if (r is ParticleSystemRenderer)
                continue;

            Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
    }


    public Enums.PowerupState getShownState()
    {
        if (state == Enums.PowerupState.OppressorMKII)
            return previousState;
        return state;
    }
}
