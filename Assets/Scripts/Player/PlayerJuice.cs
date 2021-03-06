using System.Collections;
using UnityEngine;

public class PlayerJuice : MonoBehaviour {
    [Header("Components")]
    PlayerMovement moveScript;
    PlayerJump jumpScript;
    [SerializeField] Animator myAnimator;
    [SerializeField] GameObject characterSprite;

    [Header("Components - Particles")]
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem landParticles;
    [SerializeField] private ParticleSystem afterImageParticles;
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private ParticleSystem superJumpParticles;
    [SerializeField] private ParticleSystem deathParticles;

    [Header("Components - Audio")]
    [SerializeField] AudioSource jumpSFX;
    [SerializeField] AudioSource landSFX;
    [SerializeField] AudioSource dashSFX;
    [SerializeField] AudioSource superJumpSFX;
    [SerializeField] AudioSource deathSFX;

    [Header("Settings - Squash and Stretch")]
    [SerializeField] bool squashAndStretch;
    [SerializeField, Tooltip("Width Squeeze, Height Squeeze, Duration")] Vector3 jumpSquashSettings;
    [SerializeField, Tooltip("Width Squeeze, Height Squeeze, Duration")] Vector3 landSquashSettings;
    [SerializeField, Tooltip("How powerful should the effect be?")] public float landSqueezeMultiplier;
    [SerializeField, Tooltip("How powerful should the effect be?")] public float jumpSqueezeMultiplier;
    [SerializeField] float landDrop = 1;

    [Header("Settings - Dash")]
    [SerializeField] float shakeIntensity;
    [SerializeField] float shakeDuration;

    [Header("Settings - Super Jump")]
    [SerializeField] float spinSpeed;
    [SerializeField] float spinDuration;

    [Header("Tilting")]
    [SerializeField, Tooltip("How far should the character tilt?")] public float maxTilt;
    [SerializeField, Tooltip("How fast should the character tilt?")] public float tiltSpeed;

    [Header("Calculations")]
    public float runningSpeed;
    public float maxSpeed;

    [Header("Current State")]
    public bool squeezing;
    public bool jumpSqueezing;
    public bool landSqueezing;
    public bool playerGrounded;

    void Start() {
        moveScript = GetComponent<PlayerMovement>();
        jumpScript = GetComponent<PlayerJump>();
        
    }

    void Update() {
        tiltCharacter();
        checkForLanding();
    }

    private void tiltCharacter() {
        //See which direction the character is currently running towards, and tilt in that direction
        float directionToTilt = 0;
        float v = moveScript.GetVelocity().x;
        if (v != 0) directionToTilt = v > 0 ? 1 : -1;

        //Create a vector that the character will tilt towards
        Vector3 targetRotVector = new Vector3(0, 0, Mathf.Lerp(-maxTilt, maxTilt, Mathf.InverseLerp(-1, 1, directionToTilt)));

        //And then rotate the character in that direction
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(-targetRotVector), tiltSpeed * Time.deltaTime);
    }

    private void checkForLanding() {
        if (!playerGrounded && jumpScript.onGround) {
            //By checking for this, and then immediately setting playerGrounded to true, we only run this code once when the player hits the ground 
            playerGrounded = true;

            //Play an animation, some particles, and a sound effect when the player lands
            // myAnimator.SetTrigger("Landed");
            if(landParticles != null) landParticles.Play();

            if (landSFX != null && !landSFX.isPlaying && landSFX.enabled) landSFX.Play();

            //Start the landing squash and stretch coroutine if enabled
            if (squashAndStretch && !landSqueezing && landSqueezeMultiplier > 1) {
                StartCoroutine(JumpSqueeze(landSquashSettings.x * landSqueezeMultiplier, landSquashSettings.y / landSqueezeMultiplier, landSquashSettings.z, landDrop, false));
            }

        } else if (playerGrounded && !jumpScript.onGround) {
            // Player has left the ground, so stop playing the running particles
            playerGrounded = false;
        }
    }

    public void DeathEffects() {
        characterSprite.GetComponent<SpriteRenderer>().enabled = false;
        if(deathSFX != null) deathSFX.Play();
        if(deathParticles != null) deathParticles.Play();

    }

    public void RespawnEffects() {
        characterSprite.GetComponent<SpriteRenderer>().enabled = true;
    }

    public void DashEffects() {
        if(afterImageParticles != null) afterImageParticles.Play();
        if(dashParticles != null) dashParticles.Play();
        if(dashSFX != null && dashSFX.enabled) dashSFX.Play();
        CameraShake.GetInstance().ShakeCamera(shakeIntensity, shakeDuration);
    }

    public void SuperJumpEffects() {
        if(superJumpSFX != null) superJumpSFX.Play();
        if(superJumpParticles != null) superJumpParticles.Play();
        jumpEffects(); // Maybe remove this
        StartCoroutine(SuperJumpSpin(spinSpeed, spinDuration));
    }

    public void jumpEffects() {
        //Play these effects when the player jumps, courtesy of jump script
        // myAnimator.ResetTrigger("Landed");
        // myAnimator.SetTrigger("Jump");

        if (jumpSFX != null && jumpSFX.enabled) jumpSFX.Play();

        if (squashAndStretch && !jumpSqueezing && jumpSqueezeMultiplier > 1) {
            StartCoroutine(JumpSqueeze(jumpSquashSettings.x / jumpSqueezeMultiplier, jumpSquashSettings.y * jumpSqueezeMultiplier, jumpSquashSettings.z, 0, true));
        }

        if(jumpParticles != null) jumpParticles.Play();
    }

    IEnumerator SuperJumpSpin(float speed, float duration) {
        Vector3 rotation = new Vector3(0, 0, speed);
        float t = 0;
        while(t <= 1f) {
            t += Time.deltaTime / duration;
            characterSprite.transform.Rotate(rotation);
            yield return null;
        }

        characterSprite.transform.rotation = Quaternion.identity;
    }

    IEnumerator JumpSqueeze(float xSqueeze, float ySqueeze, float seconds, float dropAmount, bool jumpSqueeze)
    {
        //We log that the player is squashing/stretching, so we don't do these calculations more than once
        if (jumpSqueeze) { jumpSqueezing = true; }
        else { landSqueezing = true; }
        squeezing = true;

        Vector3 originalSize = Vector3.one;
        Vector3 newSize = new Vector3(xSqueeze, ySqueeze, originalSize.z);

        Vector3 originalPosition = Vector3.zero;
        Vector3 newPosition = new Vector3(0, -dropAmount, 0);

        //We very quickly lerp the character's scale and position to their squashed and stretched pose...
        float t = 0f;
        while (t <= 1.0) {
            t += Time.deltaTime / 0.01f;
            characterSprite.transform.localScale = Vector3.Lerp(originalSize, newSize, t);
            characterSprite.transform.localPosition = Vector3.Lerp(originalPosition, newPosition, t);
            yield return null;
        }

        //And then we lerp back to the original scale and position at a speed dicated by the developer
        //It's important to do this to the character's sprite, not the gameobject with a Rigidbody an/or collision detection
        t = 0f;
        while (t <= 1.0) {
            t += Time.deltaTime / seconds;
            characterSprite.transform.localScale = Vector3.Lerp(newSize, originalSize, t);
            characterSprite.transform.localPosition = Vector3.Lerp(newPosition, originalPosition, t);
            yield return null;
        }

        if (jumpSqueeze) { jumpSqueezing = false; }
        else { landSqueezing = false; }
    }
}
