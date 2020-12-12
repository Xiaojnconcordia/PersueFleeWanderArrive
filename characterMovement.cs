using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class characterMovement : MonoBehaviour
{
    //public GameObject[] potentialTargets;
    public GameObject[] Characters;
    //public GameObject fleeTarget;
    float persueSpeed = 6.0f;
    float movingSlowlySpeed = 3.0f;
    float wanderSpeed = 5.0f;
    float fleeSpeed = 4.5f;
    float arriveSpeed = 6.0f;
    float arriveNearSpeed = 3.0f;
    float arriveNearRadius = 15.0f;
    float arrivalRadius = 4.0f;
    float range = 50.0f;
    float rangeOffset = 6.0f;
    float[] distanceFromPotentialTargets = { 9999, 9999, 9999, 9999};
    float[] distanceFromFrozen = { 9999, 9999, 9999, 9999};
    int fleeIndex;     
    int taggerIndex;
    int frozenIndex;
    float distanceFromTarget;
    float distanceFromNearestFrozen;
    float far = 30;  // distance greater than far is considered as a very large distance
    float near = 10; // distance less than near is considered as a very small distance

    float estimatedTime;
    Quaternion lookWhereYoureGoing;
    Vector3 targetFuturePosition;
    Vector3 goalFacing;
    float rotationDegreesPerSecond = 240;    
    Vector3 wanderDirection;     
    float timePassed = 1;
    float delay = 1;
    float stopTimeAfterBecomeTagger = 3;
    float freezeRadius = 4.0f;
    int numOfFrozen; //to trace if someone is frozen and needed to be 'unfrozen'

    



    public bool isTagger;
    public bool isBeingPersued;
    public bool isFrozen;
    public bool isNotBeingPersuedOrFrozen;


    // Start is called before the first frame update
    void Start()
    {
        //for test convenience, red is always the first tagger
        //Characters[0].GetComponent<characterMovement>().isTagger = true;
        //Characters[1].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;
        //Characters[2].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;
        //Characters[3].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;

        // the first tagger is chosen at random 
        // only let one character (red in this case) choose who the first tagger is. Otherwise each character will choose a tagger.
        if (gameObject.name == "Character_red")
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;
            }

            int r = Random.Range(0, 4); //random number among 0-3
                                        // one character chosen at random to be the tagger
            Characters[r].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = false;
            Characters[r].GetComponent<characterMovement>().isTagger = true;
        }



    }

    // Update is called once per frame
    void Update()
    {
        checkNumOfFrozen();
        checkStatus(); //check who's tagger and enable it's tagger sign
        if (isTagger)
            Persue();
        if (isBeingPersued)
            Flee();        
        if (numOfFrozen > 0 && isNotBeingPersuedOrFrozen)
            Arrive();
        if (numOfFrozen == 0 && isNotBeingPersuedOrFrozen)
            Wander();
        if (isFrozen)
            Stationary();
        
        //if (Gameover())
        //    Stationary();

    }

    void Persue()
    {
        // Debug.Log("Persue starts");
        // all other characters are potential targets. Get the distance to all other characters.
        for (int i = 0; i < Characters.Length; i++)
        {
            //if the character is frozen or itself, we do not need the distance
            if (Characters[i].GetComponent<characterMovement>().isTagger == true 
                || Characters[i].GetComponent<characterMovement>().isFrozen == true)
            {
                distanceFromPotentialTargets[i] = float.MaxValue;
            }

            else
            {
                distanceFromPotentialTargets[i] = (Characters[i].transform.position - transform.position).magnitude;                
            }
                
        }

        // find the nearest character (minimum distance)
        distanceFromTarget = Mathf.Min(distanceFromPotentialTargets);

        // find the nearest character's index        
        fleeIndex = GetIndexOfLowestValue(distanceFromPotentialTargets);

        // now potentialTargets[index] is the nearest character and being pursued
        // without this if, when all are frozen, their distance is the same (int.Maxvalue), so a frozen one can become being chased one.   
        if (Characters[fleeIndex].GetComponent<characterMovement>().isFrozen == false
            && Characters[fleeIndex].GetComponent<characterMovement>().isTagger == false)
        {
            Characters[fleeIndex].GetComponent<characterMovement>().isBeingPersued = true;
            Characters[fleeIndex].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = false;
        }

        // the last fleeing character (now is not the nearest to the tagger) should not flee anymore
        for (int i = 0; i < Characters.Length; i++)
        {
            //if it is neither being persue nor it is the tagger
            if (i != fleeIndex 
                && !Characters[i].GetComponent<characterMovement>().isTagger
                && !Characters[i].GetComponent<characterMovement>().isFrozen)
            {
                Characters[i].GetComponent<characterMovement>().isBeingPersued = false;
                Characters[i].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;
            }
        }




        //start to pursue the nearest character

        // calculate how long I can get to the target character's posotion
        estimatedTime = distanceFromTarget / persueSpeed; //how long to get to the target

        //during this estimated time, the target might move to this position:
        targetFuturePosition = Characters[fleeIndex].transform.position + Characters[fleeIndex].GetComponent<Rigidbody>().velocity * estimatedTime;

        // if the target's future position is out of the arena boundary, it should be on the opposite side.
        if (targetFuturePosition.x > range)
        {
            targetFuturePosition = new Vector3(targetFuturePosition.x - range, targetFuturePosition.y, targetFuturePosition.z);
        }
        if (targetFuturePosition.x < -range)
        {
            targetFuturePosition = new Vector3(targetFuturePosition.x + range, targetFuturePosition.y, targetFuturePosition.z);
        }

        if (targetFuturePosition.z > range)
        {
            targetFuturePosition = new Vector3(targetFuturePosition.x, targetFuturePosition.y, targetFuturePosition.z - range);
        }
        if (targetFuturePosition.z < -range)
        {
            targetFuturePosition = new Vector3(targetFuturePosition.x, targetFuturePosition.y, targetFuturePosition.z + range);
        }

        // If the character is stationary or moving very slowly then
        if (GetComponent<Rigidbody>().velocity.magnitude < movingSlowlySpeed)
        {
            //If it is a very small distance from its target
            if (distanceFromTarget < near)
            {
                //it will step there directly, even if this involves moving backward or sidestepping
                if (Characters[fleeIndex].GetComponent<characterMovement>().isBeingPersued == true)
                    GetComponent<Rigidbody>().velocity = ((targetFuturePosition - transform.position).normalized * persueSpeed);
            }

            // Else if the target is farther away
            else if (distanceFromTarget > far)
            {
                // the character will first turn on the spot to face its target
                goalFacing = (targetFuturePosition - transform.position).normalized;
                lookWhereYoureGoing = Quaternion.LookRotation(goalFacing, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationDegreesPerSecond * Time.deltaTime);

                // then move forward to reach the target 
                if (transform.rotation == lookWhereYoureGoing)
                {
                    if (Characters[fleeIndex].GetComponent<characterMovement>().isBeingPersued == true)
                        GetComponent<Rigidbody>().velocity = ((targetFuturePosition - transform.position).normalized * persueSpeed);
                }
            }
        }

        else// moving with some speed, not very slowly or is not stationary 
        {
            goalFacing = (targetFuturePosition - transform.position).normalized;
            lookWhereYoureGoing = Quaternion.LookRotation(goalFacing, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationDegreesPerSecond * Time.deltaTime);

            // persue the nearest character
            if (Characters[fleeIndex].GetComponent<characterMovement>().isBeingPersued == true)
                GetComponent<Rigidbody>().velocity = ((targetFuturePosition - transform.position).normalized * persueSpeed);
        }

        // regardless the speed, uncomment to demo
        ////If it is a very small distance from its target
        //if (distanceFromTarget < near)
        //{
        //    //it will step there directly, even if this involves moving backward or sidestepping
        //    if (Characters[fleeIndex].GetComponent<characterMovement>().isBeingPersued == true)
        //        GetComponent<Rigidbody>().velocity = ((targetFuturePosition - transform.position).normalized * persueSpeed);
        //}

        //// Else if the target is farther away
        //else if (distanceFromTarget > far)
        //{
        //    // the character will first turn on the spot to face its target
        //    goalFacing = (targetFuturePosition - transform.position).normalized;
        //    lookWhereYoureGoing = Quaternion.LookRotation(goalFacing, Vector3.up);
        //    transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationDegreesPerSecond * Time.deltaTime);

        //    // then move forward to reach the target 
        //    if (transform.rotation == lookWhereYoureGoing)
        //    {
        //        if (Characters[fleeIndex].GetComponent<characterMovement>().isBeingPersued == true)
        //            GetComponent<Rigidbody>().velocity = ((targetFuturePosition - transform.position).normalized * persueSpeed);
        //    }
        //}


        // The tagger can freeze a fleeing character if their distance is smaller than freezeRadius
        if ((transform.position - Characters[fleeIndex].transform.position).magnitude < freezeRadius)
        {
            Characters[fleeIndex].GetComponent<characterMovement>().isBeingPersued = false;
            Characters[fleeIndex].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = false;
            Characters[fleeIndex].GetComponent<characterMovement>().isFrozen = true;
            //numOfFrozen++; // This doesn't work because only in tagger's script the numOfFrozen increases. 
            
            checkNumOfFrozen();
            if (numOfFrozen == Characters.Length - 1) // all other character are frozen
            {
                resetStatus();
                GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;
                GetComponent<characterMovement>().isTagger = false;

                //the last character being frozen becomes the new tagger
                Characters[fleeIndex].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = false;
                Characters[fleeIndex].GetComponent<characterMovement>().isTagger = true;

                // move the new tagger to the center of the Map. Otherwise it is next to the old tagger and will immediately freeze it.
                Characters[fleeIndex].transform.position = Vector3.zero;
            }


        }
    }

    void Flee()
    {
        Debug.Log("Flee starts");

        for (int i = 0; i < Characters.Length; i++)
        {
            //if the character is the tagger, we record its index it
            if (Characters[i].GetComponent<characterMovement>().isTagger == true)
            {
                taggerIndex = i;
            }
        }
        // fleeing from the tagger character
        goalFacing = -(Characters[taggerIndex].transform.position - transform.position).normalized;
        lookWhereYoureGoing = Quaternion.LookRotation(goalFacing, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationDegreesPerSecond * Time.deltaTime);
        GetComponent<Rigidbody>().velocity = -((Characters[taggerIndex].transform.position - transform.position).normalized * fleeSpeed);
        
        // if a fleeing character goes off one side of the arena, then that character will reappear on the opposite side 
        if (transform.position.x > range)
        {
            transform.position = new Vector3(-range + rangeOffset, transform.position.y, transform.position.z);
            persueSpeed++;
            fleeSpeed--;
            // sometimes Fleeing character will always flee through the boundary and the tagger can never get it
            // so every time it goes through the boundary, we decrease fleer's speed
        }
        if (transform.position.x < -range)
        {
            transform.position = new Vector3(range - rangeOffset, transform.position.y, transform.position.z);            
            fleeSpeed--;
        }

        if (transform.position.z > range)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, -range + rangeOffset);            
            fleeSpeed--;
        }
        if (transform.position.z < -range)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, range - rangeOffset);            
            fleeSpeed--;
        }
    }

    void Wander()
    {
        Debug.Log("Wander starts");
        timePassed += Time.deltaTime;
        if (timePassed > delay)
        {
            wanderDirection = new Vector3(Random.Range(-1.0f, 1.0f), 0, Random.Range(-1.0f, 1.0f));
            timePassed = 0;
        }
        goalFacing = wanderDirection.normalized;
        lookWhereYoureGoing = Quaternion.LookRotation(goalFacing, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationDegreesPerSecond * Time.deltaTime);

        GetComponent<Rigidbody>().velocity = (wanderDirection.normalized * wanderSpeed);

        // if a character goes off one side of the arena, then that character will reappear on the opposite side ( 
        if (transform.position.x > range)
        {
            transform.position = new Vector3(-range + rangeOffset, transform.position.y, transform.position.z);
        }
        if (transform.position.x < -range)
        {
            transform.position = new Vector3(range - rangeOffset, transform.position.y, transform.position.z);
        }

        if (transform.position.z > range)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, -range + rangeOffset);
        }
        if (transform.position.z < -range)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, range - rangeOffset);
        }
    }

    void Arrive()
    {
        Debug.Log("Arrive starts");

        // all other characters are potential targets. Get the distance to all other characters.
        for (int i = 0; i < Characters.Length; i++)
        {
            //if the character is frozen, we need the distance to find which frozen one is the nearest
            if (Characters[i].GetComponent<characterMovement>().isFrozen == true)
            {

                distanceFromFrozen[i] = (Characters[i].transform.position - transform.position).magnitude;                
            }
            else // if it is not frozen, we don't need the distance
            {
                distanceFromFrozen[i] = 9999;
            }
        }

        // find the nearest frozen one (minimum distance)
        distanceFromTarget = Mathf.Min(distanceFromFrozen);

        // find the nearest character's index
        frozenIndex = GetIndexOfLowestValue(distanceFromFrozen);
        Debug.Log("frozenIndex: " + frozenIndex);




        distanceFromNearestFrozen = (Characters[frozenIndex].transform.position - transform.position).magnitude;

        goalFacing = (Characters[frozenIndex].transform.position - transform.position).normalized;
        lookWhereYoureGoing = Quaternion.LookRotation(goalFacing, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, rotationDegreesPerSecond * Time.deltaTime);

        // if the frozen one is out of the near radius, the character move with arriveSpeed
        if (distanceFromNearestFrozen > arriveNearRadius)
        {
            Debug.Log("Outside Near Radius " + distanceFromTarget);
            GetComponent<Rigidbody>().velocity = ((Characters[frozenIndex].transform.position - transform.position).normalized * arriveSpeed);
        }

        // if the frozen one is in the near radius but out of the arrive radius, the character move with arriveNearSpeed
        else if ((Characters[frozenIndex].transform.position - transform.position).magnitude > arrivalRadius)
        {
            Debug.Log("Inside Near Radius " + distanceFromTarget);
            GetComponent<Rigidbody>().velocity = ((Characters[frozenIndex].transform.position - transform.position).normalized * arriveNearSpeed);
        }

        // if the frozen one is in the arrive radius, it is 'unfrozen'
        else
        {
            Debug.Log("A character is unfrozen");
            Characters[frozenIndex].GetComponent<characterMovement>().isFrozen = false;
            Characters[frozenIndex].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;
            //numOfFrozen--;  //This doesn't work because only in the arriver's script the numOfFrozen decreases.
           
        }

        
    }


    void Stationary()
    {
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        lookWhereYoureGoing = Quaternion.LookRotation(Vector3.zero);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookWhereYoureGoing, 0 * Time.deltaTime);
    }

    void checkNumOfFrozen()
    {
        int NumOfFrozenNow = 0; //should be cleared to 0 before check. 
        for (int i = 0; i < Characters.Length; i++)
        {            
            if (Characters[i].GetComponent<characterMovement>().isFrozen == true)
            {
                NumOfFrozenNow++;
            }
        }
        numOfFrozen = NumOfFrozenNow;
    }

    void checkStatus()
    {
        if (this.GetComponent<characterMovement>().isTagger)
        {
            transform.GetChild(1).gameObject.SetActive(true);
        }
    }

    void resetStatus() 
    {
        for (int i = 0; i < Characters.Length; i++)
        {
            numOfFrozen = 0;
            Characters[i].GetComponent<characterMovement>().isTagger = false;
            Characters[i].GetComponent<characterMovement>().isBeingPersued = false;
            Characters[i].GetComponent<characterMovement>().isFrozen = false;
            Characters[i].GetComponent<characterMovement>().isNotBeingPersuedOrFrozen = true;
            transform.GetChild(1).gameObject.SetActive(false);
        }
    }


    bool Gameover()
    {
        for (int i = 0; i < Characters.Length; i++)
        {
            if (Characters[i].GetComponent<characterMovement>().isFrozen != isTagger
                && Characters[i].GetComponent<characterMovement>().isFrozen != true)
                return false;
        }
        Debug.Log("GameOver");
        return true;
    }



        int GetIndexOfLowestValue(float[] arr)
    {
        float value = float.PositiveInfinity;
        int index = -1;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] < value)
            {
                index = i;
                value = arr[i];
            }
        }
        return index;
    }
}
