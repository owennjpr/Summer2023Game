using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// using UnityStandardAssets.Characters.FirstPerson;
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

    private bool warping;

    //movement Ability variables
    public PlayerMovement controller;
    public PlayerCam cameraController;

    public Image slowfallMask;
    private Color slowfallMaskColor;
    private bool slowfallMaskActive;

    private bool highJumpMaskActive;
    public Image highjumpMask;
    private Color highjumpColor;
    [SerializeField] private Image LightMask;

    public Vector3 playerSpawnPoint;
    [SerializeField] private Transform MainCenter;
    

    // game progress markers
    public bool hasYellowPower;
    public bool hasBluePower;
    public bool hasPurplePower;
    public bool hasRedPower;
    private int numPowersFound;

    public GameObject DialUIElems;

    public GameObject lightSequenceUIElems;
    public ZoneState currZone;
    public enum ZoneState {
        SouthZone,
        WestZone,
        EastZone,
        NorthZone,
        EndingZone
    }

    // pause menu & other ui
    public bool paused;
    [SerializeField] private GameObject pausemenu;
    [SerializeField] private GameObject optionsmenu;
    
    [SerializeField] private popupText popup;


    // colors
    private Color yellowColor;
    private Color blueColor;
    private Color purpleColor;
    private Color redColor;




    //Debugging toggle
    [SerializeField] private bool Debugging;
    // public Transform targetSphere;
    // Start is called before the first frame update
    void Start()
    {

        playerSpawnPoint = new Vector3(0, 2, 0);
        player = GameObject.FindWithTag("Player");
        playerVector = new Vector2(player.transform.position.x, player.transform.position.z);
        
        warping = false;

        findNewCenter();

        fadeColor = fullScreenFade.color;
        DynamicFadeColor = DynamicFullScreenFade.color;
        DynamicFadeColor.a = 0.0f;
        fadeColor.a = 0.0f;
        
        controller = player.GetComponent<PlayerMovement>();
        cameraController = GameObject.FindWithTag("MainCamera").GetComponent<PlayerCam>();
        // slowfallActivated = false;
        slowfallMaskColor = slowfallMask.color;
        slowfallMaskColor.a = 0.0f;
        slowfallMaskActive = false;

        highJumpMaskActive = false;
        highjumpColor = highjumpMask.color;
        highjumpColor.a = 0.0f;


        if (!Debugging) {
            hasYellowPower = false;
            hasBluePower = false;
            hasPurplePower = false;
            hasRedPower = false;
            numPowersFound = 0;
            currZone = ZoneState.SouthZone;
        
            readSaveData();
        }
        
        
        paused = false;
        cameraController.paused = false;
        pausemenu.SetActive(false);
        optionsmenu.SetActive(false);


        yellowColor = new Color(1f, 0.95f, 0.7f);
        blueColor = new Color(0.7f, 1f, 1f);
        purpleColor = new Color(0.9f, 0.7f, 1f);
        redColor = new Color(1f, 0.7f, 0.7f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            pausePlay();
        }

        // if (Input.GetKeyDown("r")) {
        //     Debug.Log("rrr");
        //     turnTowardsTarget(targetSphere);
        // }

        // Debug.Log(CenterPoint.gameObject.name);

        if (CenterPoint == null & !warping) {
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
            if (x > 1.5f & CenterPoint != null & !warping) {
                // Debug.Log("returnn");
                StartCoroutine(oobReturn());
            }
        } else {
            fadeColor.a = 0.0f;
        }
        fullScreenFade.color = fadeColor;
    }

    


    private void panicFindCenter() {
        
        Debug.Log("panic find new center");
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

        if (CenterPoint == null & StableCenterPoint != null) {
            StartCoroutine(oobReturn());
            CenterPoint = StableCenterPoint;
        }

        Debug.Log("new center " + CenterPoint.gameObject.name);

    }

    private IEnumerator oobReturn() {
        
        // controller.haltWalk = true;
        if (CenterPoint == StableCenterPoint) {
            // controller.flipped = !(controller.flipped);
            // turnTowardsTarget(CenterPoint);
            cameraController.yOverride += 180;
            player.transform.position = Vector3.MoveTowards(player.transform.position, CenterPoint.position, fadeRange/2);
        } else {
            Vector3 stablePos = StableCenterPoint.position;
            player.transform.position = stablePos + StableCenterPoint.GetComponent<CenterPointControl>().relativeSpawnPos;
        }
        
        yield return new WaitForSeconds(0.05f);
        // controller.haltWalk = false;
    }

    // figures out what center point player in rn so can figure out what should be able to do
    // called at start() but also when enter or leave centers
    public void findNewCenter() {
        //stop rendering current centerpoint
        Transform oldCenter = null;
        if (StableCenterPoint != null) {
            oldCenter = StableCenterPoint;
        }
        // if (CenterPoint != null && CenterPoint != StableCenterPoint) {
        //     CenterPoint.GetComponent<CenterPointControl>().deactivate();
        // }
        
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
        } else {
            StartCoroutine(teleportToDestination());
        }
        RenderSettings.fogDensity = 1.2f/fadeStartDistance;
        // Debug.Log("new center " + CenterPoint.gameObject.name);
        
        if (oldCenter != null && oldCenter != CenterPoint) {
            oldCenter.GetComponent<CenterPointControl>().deactivate();
        }
        if (StableCenterPoint != null) {
            StableCenterPoint.GetComponent<CenterPointControl>().activate();
        }
    }
    
    // when the player is in a moving light
    public IEnumerator teleportToDestination() {
        // Debug.Log("in a mover");
        yield return new WaitForSeconds(3.0f);
        if (CenterPoint.gameObject.GetComponent<CenterPointControl>().isMoving & !warping) {
            float distance = Vector2.Distance(centerVector, playerVector);
            
            //if the player is still in range after 3 seconds, start the teleport
            if(fadeEndDistance >= distance) {
                warping = true;

                var (destination, currColorID) = CenterPoint.parent.GetComponent<ControlledLightMove>().getDestinationAndColor();
                

                Color LightMaskColor = Color.white;
                switch (currColorID) {
                    case 0:
                        LightMaskColor = yellowColor;
                        break;
                    case 1:
                        LightMaskColor = blueColor;
                        break;
                    case 2:
                        LightMaskColor = purpleColor;
                        break;
                    case 3:
                        LightMaskColor = redColor;
                        break;
                }
                LightMaskColor.a = 0f;
                while (LightMask.color.a < 1f) {
                    LightMaskColor.a += Time.deltaTime * 1.5f;
                    LightMask.color = LightMaskColor;
                    yield return null;
                }

                StartCoroutine(warpPlayer(destination));
                yield return new WaitForSeconds(0.2f);
                warping = false;

                turnTowardsTarget(CenterPoint);
                lightSequenceUIElems.GetComponent<uiLightSequence>().updateSequence(currColorID);
                
                while (LightMask.color.a > 0f) {
                    LightMaskColor.a -= Time.deltaTime * 1.5f;
                    LightMask.color = LightMaskColor;
                    yield return null;
                }
                foreach (Transform child in transform) {
                    Destroy(child.gameObject);
                }
            }
        }

    }

    private void turnTowardsTarget(Transform target) {
        
        Vector3 directionToTarget = target.position - player.transform.position;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        float yRotationDifference = targetRotation.eulerAngles.y - player.transform.GetChild(1).rotation.eulerAngles.y;
        // float xRotationDifference = targetRotation.eulerAngles.x - cameraController.transform.rotation.eulerAngles.x;
        // Debug.Log("target: " + targetRotation.eulerAngles.y + "current: " + player.transform.rotation.eulerAngles.y + "difference: " + rotationDifference);
    
        cameraController.yOverride += yRotationDifference;
        // cameraController.xOverride += xRotationDifference;
    }

    public void checkFade() {
        if (fadeColor.a > 0.0f) {
            StartCoroutine(manualFade(fadeColor.a));
            // Debug.Log("Howdy");
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
        // Debug.Log("huh???");
        // controller.haltWalk = true;
        yield return new WaitForSeconds(1f);
        player.transform.position = MainCenter.position + new Vector3(0, 1.2f, 0);
        findNewCenter();
        yield return new WaitForSeconds(0.1f);
        // controller.haltWalk = false;
    
    }
    // -------------------------------------------------------------------------------------------
    // PLAYER POWERS
    // PLAYER POWERS
    // PLAYER POWERS
    // PLAYER POWERS
    // -------------------------------------------------------------------------------------------

    public void slowfall() {
        // Debug.Log("Slowly falling");
        // slowfallActivated = true;
        StartCoroutine(fadeinSlowfall());
        controller.readySlowfall();
    }

    public void highjump() {
        // Debug.Log("high jump");
        // slowfallDelay = 0.9f;
        controller.readyHighJump();
        StartCoroutine(fadeinHighJump());

    }


    private IEnumerator fadeinSlowfall() {
        if (!slowfallMaskActive) {
            slowfallMaskActive = true;
            while (slowfallMask.color.a < 0.10f) {
                slowfallMaskColor.a += Time.deltaTime;
                slowfallMask.color = slowfallMaskColor;
                yield return null;
            }
        } else {
            yield return null;
        }
    }

    private IEnumerator fadeinHighJump() {
        if (!highJumpMaskActive) {
            highJumpMaskActive = true; 
            while (highjumpMask.color.a < 0.25f) {
                highjumpColor.a += Time.deltaTime;
                highjumpMask.color = highjumpColor;
                yield return null;
            }
        } else {
            yield return null;
        }
    }

    public IEnumerator fadeoutSlowfall() {
        if (slowfallMaskActive) {
            slowfallMaskActive = false;
            controller.gravityMultiplier = 3;
            while (slowfallMask.color.a > 0) {
                slowfallMaskColor.a -= Time.deltaTime;
                slowfallMask.color = slowfallMaskColor;
                yield return null;
            }
        } else {
            yield return null;
        }
    }

    public IEnumerator fadeoutHighJump() {
        if (highJumpMaskActive) {
            highJumpMaskActive = false;
            while (highjumpMask.color.a > 0) {
                highjumpColor.a -= Time.deltaTime/2;
                highjumpMask.color = highjumpColor;
                yield return null;
            }
        } else {
            yield return null;
        }
    }


    public IEnumerator falling() {
        
        Color LightMaskColor = LightMask.color;
        while (LightMask.color.a < 1f) {
            LightMaskColor.a += Time.deltaTime * 2;
            LightMask.color = LightMaskColor;
            yield return null;
        }

        StartCoroutine(warpPlayer(playerSpawnPoint));

        yield return new WaitForSeconds(0.2f);

        while (LightMask.color.a > 0f) {
            LightMaskColor.a -= Time.deltaTime * 1.5f;
            LightMask.color = LightMaskColor;
            yield return null;
        }
        
    }

    


    public void newPowerFound() {
        numPowersFound++;
        if (numPowersFound - 2 >= 0) {
            MainCenter.GetChild(numPowersFound - 2).gameObject.SetActive(false);
        }
        MainCenter.GetChild(numPowersFound - 1).gameObject.SetActive(true);
        clearDialUI();
        switch (numPowersFound) {
            case 1:
                Debug.Log("Got Yellow");
                StartCoroutine(popup.CenterPopupAppear("NEW POWER UNLOCKED","Throw Yellow Lights to Interact With Objects" , 3));
                hasYellowPower = true;
                currZone = ZoneState.WestZone;
                break;
            case 2:
                Debug.Log("Got Blue");
                StartCoroutine(popup.CenterPopupAppear("NEW POWER UNLOCKED", "Absorb Blue Lights to Slow Your Fall", 3));
                hasBluePower = true;
                currZone = ZoneState.EastZone;
                break;
            case 3:
                Debug.Log("Got Purple");
                StartCoroutine(popup.CenterPopupAppear("NEW POWER UNLOCKED", "Absorb Purple Lights to Jump Higher", 3));
                hasPurplePower = true;
                currZone = ZoneState.NorthZone;
                break;
            case 4:
                Debug.Log("Got Red");
                StartCoroutine(popup.CenterPopupAppear("NEW POWER UNLOCKED", "Absorb Red Lights to Do Something", 3));
                hasRedPower = true;
                currZone = ZoneState.EndingZone;
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
            cameraController.paused = true;
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
        cameraController.paused = false;
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
        // Debug.Log(savePos);
        writeSaveData(savePos);
        SceneManager.LoadScene("Main Menu");
    }

    // -------------------------------------------
    // Game State
    // -------------------------------------------

    public void AddNewDialToUI(int id) {
        Debug.Log("adding new ui element");
        DialUIElems.transform.GetChild(id).gameObject.SetActive(true);
    }

    private void clearDialUI() {
        for (int i = 0; i < DialUIElems.transform.childCount; i++) {
            DialUIElems.transform.GetChild(i).gameObject.SetActive(false);
        }
        // lightSequenceUIElems.GetComponent<uiLightSequence>().clear();
    }

    public void showColorSequence(List<int> colorSequence) {
        // Debug.Log(colorSequence.Count);
        lightSequenceUIElems.GetComponent<uiLightSequence>().newSequence(colorSequence);
    }

    private void readSaveData() {
        if (!SaveData.isNewGame) 
        {
            hasYellowPower = SaveData.hasYellowPower;
            hasBluePower = SaveData.hasBluePower;
            hasPurplePower = SaveData.hasPurplePower;
            hasRedPower = SaveData.hasRedPower;

            if (hasRedPower) {
                numPowersFound = 4;
                currZone = ZoneState.EndingZone;
            } else if (hasPurplePower) {
                numPowersFound = 3;
                currZone = ZoneState.NorthZone;
            } else if (hasBluePower) {
                numPowersFound = 2;
                currZone = ZoneState.EastZone;
            } else if (hasYellowPower) {
                numPowersFound = 1;
                currZone = ZoneState.WestZone;
            } else {
                numPowersFound = 0;
                currZone = ZoneState.SouthZone;
            }

            if (numPowersFound > 0) {
                MainCenter.GetChild(numPowersFound - 1).gameObject.SetActive(true);
            }
        } else {
            SaveData.hasYellowPower = false;
            SaveData.hasBluePower = false;
            SaveData.hasPurplePower = false;
            SaveData.hasRedPower = false;
        }
        // Debug.Log(SaveData.spawnPoint);
        StartCoroutine(warpPlayer(SaveData.spawnPoint));
    }

    //teleport the player by overriding the walk controller
    private IEnumerator warpPlayer(Vector3 pos) {
        // controller.haltWalk = true;
        yield return new WaitForSeconds(0.1f);
        
        player.transform.position = pos;
        findNewCenter();
        Debug.Log(player.transform.position);
        yield return new WaitForSeconds(0.1f);
        
        // controller.haltWalk = false;
        
    }

    private void writeSaveData(Vector3 position) {
        SaveData.hasYellowPower = hasYellowPower;
        SaveData.hasBluePower = hasBluePower;
        SaveData.hasPurplePower = hasPurplePower;
        SaveData.hasRedPower = hasRedPower;
        SaveData.spawnPoint = position;
    }

}