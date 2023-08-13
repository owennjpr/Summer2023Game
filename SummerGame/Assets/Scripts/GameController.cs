using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.SceneManagement;


public class GameController : MonoBehaviour
{
    public Transform CenterPoint;
    public Transform StableCenterPoint;

    public GameObject player;
    private Vector2 playerVector;
    private Vector2 centerVector;
    public Image fullScreenFade;
    public Image DynamicFullScreenFade;
    private Color fadeColor;
    private Color DynamicFadeColor;
    public float fadeStartDistance;
    public float fadeEndDistance;
    private float fadeRange;
    private bool movingCenter;

    //movement Ability variables
    public FirstPersonController controller;

    private bool slowfallActivated;
    private bool slowfallInUse;
    public Image slowfallMask;
    private Color slowfallMaskColor;
    private float slowfallDelay;

    private bool highjumpActivated;
    private bool highjumpInUse;
    public Image highjumpMask;
    private Color highjumpColor;
    [SerializeField] private Image FallingMask;

    public Vector3 playerSpawnPoint;
    [SerializeField] private Transform MainCenter;


    // game progress markers
    public bool hasYellowPower;
    public bool hasBluePower;
    public bool hasPurplePower;
    public bool hasRedPower;
    private int numPowersFound;

    // pause menu
    public bool paused;
    [SerializeField] private GameObject pausemenu;
    [SerializeField] private GameObject optionsmenu;

    // Start is called before the first frame update
    void Start()
    {

        playerSpawnPoint = new Vector3(0, -5, 0);
        player = GameObject.FindWithTag("Player");
        playerVector = new Vector2(player.transform.position.x, player.transform.position.z);
        
        findNewCenter();

        fadeColor = fullScreenFade.color;
        DynamicFadeColor = DynamicFullScreenFade.color;
        DynamicFadeColor.a = 0.0f;
        fadeColor.a = 0.0f;
        
        controller = player.GetComponent<FirstPersonController>();
        slowfallActivated = false;
        slowfallInUse = false;
        slowfallMaskColor = slowfallMask.color;
        slowfallMaskColor.a = 0.0f;
        slowfallDelay = 0.5f;

        highjumpActivated = false;
        highjumpInUse = false;
        highjumpColor = highjumpMask.color;
        highjumpColor.a = 0.0f;

        // UNCOMMENT THIS BACK !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // hasYellowPower = false;
        // hasBluePower = false;
        // hasPurplePower = false;
        // hasRedPower = false;
        numPowersFound = 0;
        
        readSaveData();
        
        paused = false;
        pausemenu.SetActive(false);
        optionsmenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            pausePlay();
        }
        
        if (CenterPoint == null) {
            panicFindCenter();
        }
        if (movingCenter) {
            centerVector = new Vector2(CenterPoint.position.x, CenterPoint.position.z);
        }
        playerVector = new Vector2(player.transform.position.x, player.transform.position.z);
        
        float distance = Vector2.Distance(centerVector, playerVector);

        if (distance >= fadeStartDistance) {
            float x = (((distance - fadeStartDistance)/fadeRange));
            fadeColor.a = Mathf.Min(x, 1.0f);
            if (x > 1.5f) {
                StartCoroutine(oobReturn());
            }
        } else {
            fadeColor.a = 0.0f;
        }
        fullScreenFade.color = fadeColor;
    }

    


    private void panicFindCenter() {
        
        GameObject[] centers = GameObject.FindGameObjectsWithTag("centerpoint");

        foreach(GameObject c in centers) {
            Vector2 currCenterVector = new Vector2(c.transform.position.x, c.transform.position.z);
            float tempdistance = Vector2.Distance(currCenterVector, playerVector);
            
            if (tempdistance <= c.GetComponent<CenterPointControl>().endDistance) {
                CenterPoint = c.transform;
                fadeStartDistance = CenterPoint.gameObject.GetComponent<CenterPointControl>().startDistance;
                fadeEndDistance = CenterPoint.gameObject.GetComponent<CenterPointControl>().endDistance;
                fadeRange = fadeEndDistance - fadeStartDistance;

                centerVector = new Vector2(CenterPoint.position.x, CenterPoint.position.z);
                movingCenter = CenterPoint.gameObject.GetComponent<CenterPointControl>().isMoving;
                if (!movingCenter) {
                    StableCenterPoint = CenterPoint;
                }
                RenderSettings.fogDensity = 1.2f/fadeStartDistance;
            }
        }

        if (CenterPoint == null) {
            StartCoroutine(oobReturn());
            CenterPoint = StableCenterPoint;
        }
    }

    private IEnumerator oobReturn() {
        Debug.Log("WOAH");
        controller.haltWalk = true;
        if (CenterPoint == StableCenterPoint) {
            controller.flipped = !(controller.flipped);
            player.transform.position = Vector3.MoveTowards(player.transform.position, CenterPoint.position, fadeRange/2);
        } else {
            Vector3 stablePos = StableCenterPoint.position;
            player.transform.position = stablePos + StableCenterPoint.GetComponent<CenterPointControl>().relativeSpawnPos;
        }
        
        yield return new WaitForSeconds(0.05f);
        controller.haltWalk = false;
    }

    // figures out what center point player in rn so can figure out what should be able to do
    // called at start() but also when enter or leave centers
    public void findNewCenter() {
        GameObject[] centers = GameObject.FindGameObjectsWithTag("centerpoint");
        GameObject[] activeCenters = {null, null};

        // for each center checks if player within range adds to activeCenters array
        // player should never be in more than two centers 
        foreach(GameObject c in centers) {
            Vector2 currCenterVector = new Vector2(c.transform.position.x, c.transform.position.z);
            float distance = Vector2.Distance(currCenterVector, playerVector);
            
            if (distance <= c.GetComponent<CenterPointControl>().startDistance) {
                if(activeCenters[0] == null) {
                    activeCenters[0] = c;
                } else {
                    activeCenters[1] = c;
                }
            }
        }

        if (activeCenters[0] == null) {
            return;
        } else if (activeCenters[1] == null) {
            CenterPoint = activeCenters[0].transform;
        } else {
            float c1range = activeCenters[0].GetComponent<CenterPointControl>().startDistance;
            float c2range = activeCenters[1].GetComponent<CenterPointControl>().startDistance;

            // if in two overlapping always puts you in bigger one
            if (c1range > c2range) {
                CenterPoint = activeCenters[0].transform;
            } else {
                CenterPoint = activeCenters[1].transform;
            }
        }

        fadeStartDistance = CenterPoint.gameObject.GetComponent<CenterPointControl>().startDistance;
        fadeEndDistance = CenterPoint.gameObject.GetComponent<CenterPointControl>().endDistance;
        fadeRange = fadeEndDistance - fadeStartDistance;

        centerVector = new Vector2(CenterPoint.position.x, CenterPoint.position.z);
        movingCenter = CenterPoint.gameObject.GetComponent<CenterPointControl>().isMoving;
        if (!movingCenter) {
            StableCenterPoint = CenterPoint;
        }
        RenderSettings.fogDensity = 1.2f/fadeStartDistance;
    }
    
    public void checkFade() {
        if (fadeColor.a > 0.0f) {
            StartCoroutine(manualFade(fadeColor.a));
            Debug.Log("Howdy");
        }
    }

    private IEnumerator manualFade(float startingAlpha) {
        float currAlpha = startingAlpha;
        while (currAlpha > 0.0f) {
            // Debug.Log(currAlpha);
            DynamicFadeColor.a = currAlpha;
            DynamicFullScreenFade.color = DynamicFadeColor;
            currAlpha -= 0.01f;
            yield return new WaitForSeconds(0.01f);
        }
        DynamicFadeColor.a = 0.0f;
    }

    public IEnumerator manualFadeIN() {
        float currAlpha = 0;
        while (currAlpha < 1.0f) {
            DynamicFadeColor.a = currAlpha;
            DynamicFullScreenFade.color = DynamicFadeColor;
            currAlpha += Time.deltaTime;
            yield return null;
        }
        DynamicFadeColor.a = 1.0f;
        Debug.Log("huh???");
        controller.haltWalk = true;
        yield return new WaitForSeconds(1f);
        player.transform.position = MainCenter.position + new Vector3(0, 1.2f, 0);
        findNewCenter();
        yield return new WaitForSeconds(0.1f);
        controller.haltWalk = false;
    
    }
    // -------------------------------------------------------------------------------------------
    // PLAYER POWERS
    // PLAYER POWERS
    // PLAYER POWERS
    // PLAYER POWERS
    // -------------------------------------------------------------------------------------------

    public void slowfall() {
        // Debug.Log("Slowly falling");
        slowfallActivated = true;
        StartCoroutine(fadeinSlowfall());
    }

    public void highjump() {
        // Debug.Log("high jump");
        highjumpActivated = true;
        controller.m_JumpSpeed = 22;
        slowfallDelay = 0.9f;

        StartCoroutine(fadeinHighJump());

    }


    public void playerJumped() 
    {
        // Debug.Log("jumped");
        if (slowfallActivated) {
            slowfallInUse = true;
            StartCoroutine(changeGrav());
        }
        if (highjumpActivated) {
            StartCoroutine(fadeoutHighJump());
            highjumpActivated = false;
            highjumpInUse = true;
        }
    }
    
    public void playerLanded() 
    {
        // Debug.Log("landed");
        if (slowfallInUse) {
            slowfallActivated = false;
            slowfallInUse = false;

            StartCoroutine(fadeoutSlowfall());
        }

        if (highjumpInUse) {
            highjumpInUse = false;
            controller.m_JumpSpeed = 10;
            slowfallDelay = 0.5f;
        }
    }

    private IEnumerator fadeinSlowfall() {
        while (slowfallMask.color.a < 0.10f) {
            slowfallMaskColor.a += Time.deltaTime;
            slowfallMask.color = slowfallMaskColor;
            yield return null;
        }
    }

    private IEnumerator fadeinHighJump() {
        while (highjumpMask.color.a < 0.25f) {
            highjumpColor.a += Time.deltaTime;
            highjumpMask.color = highjumpColor;
            yield return null;
        }
    }

    private IEnumerator fadeoutSlowfall() {
        controller.m_GravityMultiplier = 2;
        while (slowfallMask.color.a > 0) {
            slowfallMaskColor.a -= Time.deltaTime;
            slowfallMask.color = slowfallMaskColor;
            yield return null;
        }
    }

    private IEnumerator fadeoutHighJump() {
        while (highjumpMask.color.a > 0) {
            highjumpColor.a -= Time.deltaTime/2;
            highjumpMask.color = highjumpColor;
            yield return null;
        }
    }

    private IEnumerator changeGrav() {
        yield return new WaitForSeconds(slowfallDelay);
        if (slowfallInUse) {
            controller.m_GravityMultiplier = 0.2f;
        }
    }


    public IEnumerator falling() {
        
        Color FallingMaskColor = FallingMask.color;
        while (FallingMask.color.a < 1f) {
            FallingMaskColor.a += Time.deltaTime * 2;
            FallingMask.color = FallingMaskColor;
            yield return null;
        }

        StartCoroutine(warpPlayer(playerSpawnPoint));

        yield return new WaitForSeconds(0.2f);

        while (FallingMask.color.a > 0f) {
            FallingMaskColor.a -= Time.deltaTime * 1.5f;
            FallingMask.color = FallingMaskColor;
            yield return null;
        }
        
    }

    


    public void newPowerFound() {
        numPowersFound++;
        if (numPowersFound - 2 >= 0) {
            MainCenter.GetChild(numPowersFound - 2).gameObject.SetActive(false);
        }
        MainCenter.GetChild(numPowersFound - 1).gameObject.SetActive(true);

        switch (numPowersFound) {
            case 1:
                Debug.Log("Got Yellow");
                hasYellowPower = true;
                break;
            case 2:
                Debug.Log("Got Blue");
                hasBluePower = true;
                break;
            case 3:
                Debug.Log("Got Purple");
                hasPurplePower = true;
                break;
            case 4:
                Debug.Log("Got Red");
                hasRedPower = true;
                break;
        }
    }

    // -------------------------------------------------------------------------------------------
    // PAUSE MENU FUNCTIONS
    // PAUSE MENU FUNCTIONS
    // PAUSE MENU FUNCTIONS
    // -------------------------------------------------------------------------------------------

    private void pausePlay() {
        if (!paused) {
            pausemenu.SetActive(true);
            Time.timeScale = 0;
            paused = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;

        } else {
            resume();
        }
    }

    public void resume() {
        pausemenu.SetActive(false);
        Time.timeScale = 1;
        paused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void options() {
        pausemenu.SetActive(false);
        optionsmenu.SetActive(true);
    }

    public void options_back() {
        optionsmenu.SetActive(false);
        pausemenu.SetActive(true);
    }

    public void exit() {
        Time.timeScale = 1;

        Vector3 savePos = StableCenterPoint.position + StableCenterPoint.GetComponent<CenterPointControl>().relativeSpawnPos + new Vector3(0, 1, 0);
        foreach (Transform child in StableCenterPoint.GetChild(4))
        {
            if (child.tag == "Save Point") {
                savePos = playerSpawnPoint;
                break;
            }
        }
        Debug.Log(savePos);
        writeSaveData(savePos);
        SceneManager.LoadScene("Main Menu");
    }



    private void readSaveData() {
        hasYellowPower = SaveData.hasYellowPower;
        hasBluePower = SaveData.hasBluePower;
        hasPurplePower = SaveData.hasPurplePower;
        hasRedPower = SaveData.hasRedPower;

        if (hasRedPower) {
            numPowersFound = 4;
        } else if (hasPurplePower) {
            numPowersFound = 3;
        } else if (hasBluePower) {
            numPowersFound = 2;
        } else if (hasYellowPower) {
            numPowersFound = 1;
        } else {
            numPowersFound = 0;
        }

        //MainCenter.GetChild(numPowersFound - 1).gameObject.SetActive(true);
        Debug.Log(SaveData.spawnPoint);

        StartCoroutine(warpPlayer(SaveData.spawnPoint));
        
    }

    private IEnumerator warpPlayer(Vector3 pos) {
        controller.haltWalk = true;
        yield return new WaitForSeconds(0.1f);
        
        player.transform.position = pos;
        findNewCenter();
        yield return new WaitForSeconds(0.1f);
        Debug.Log(player.transform.position);
        controller.haltWalk = false;
        
    }

    private void writeSaveData(Vector3 position) {
        SaveData.hasYellowPower = hasYellowPower;
        SaveData.hasBluePower = hasBluePower;
        SaveData.hasPurplePower = hasPurplePower;
        SaveData.hasRedPower = hasRedPower;
        SaveData.spawnPoint = position;
    }

}