using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Enemy AI written by Hung Bui
enum AIState {
    Roaming, Aggressive, Retreating, Shooting
}
public class EnemyAI : MonoBehaviour
{
    public ScriptableEnemyAI ai; //AI Object that stores info 
    EnemyMove MovementScript; //movement script that moves character

    Combat CombatScript; //combat script that deals with combat

    AIState state; //current state: Roaming, Aggressive, Retreating, Shooting

    public Transform target; //when detecting a player, target is that player

    public Vector2 movePos; //Next Position to move to

    public float timeSinceLastAction; //time since the last action(may separate later)

    private Vector2 roomCenter;

    private bool isAlive = true;

    private bool facingRight = true;

    void Start()
    {
        MovementScript = this.GetComponent<EnemyMove>();
        CombatScript = this.GetComponent<Combat>();
        state = AIState.Roaming;
        movePos = this.transform.position;
        timeSinceLastAction = 0;
        
    }
    /*
    void Update() //temporary, delete later once called in EnemyManager script
    {
        OnUpdate(new pos(0,0)); //DELETE LATER(call OnUpdate through the EnemyManager Script!)
    }
    */
    void OnCollisionEnter2D(Collision2D col){ //for now, any collisions will trigger a movement reset
        movePos = this.transform.position;//movement reset
    }
    
    public void OnUpdate(pos roomPos)
    {
        
        if (target != null){
            if (!facingRight && target.transform.position.x - this.transform.position.x > 0){
                facingRight = true;
                this.transform.localScale = new Vector2(1, 1);
            }
            else if (facingRight && target.transform.position.x - this.transform.position.x < 0)
            {
                facingRight = false;
                this.transform.localScale = new Vector2(-1, 1);
            }
        }
        else if (!facingRight && (movePos.x - this.transform.position.x > 0
        || (target !=null &&  target.transform.position.x - this.transform.position.x > 0)))
        {
            facingRight = true;
            this.transform.localScale = new Vector2(1, 1);
        }
        else if (facingRight && movePos.x - this.transform.position.x < 0
        || (target !=null &&  target.transform.position.x - this.transform.position.x < 0))
        {
            facingRight = false;
            this.transform.localScale = new Vector2(-1, 1);
        }




        if (roomCenter.x != roomPos.x * 8 || roomCenter.y != roomPos.y * 8){
            roomCenter = new Vector2(roomPos.x * 8, roomPos.y * 8);
        }
        //Debug.Log("ROOM CENTER IS: " + roomCenter.x.ToString() +  "," + roomCenter.y.ToString());
     
        switch (state){
            
            case AIState.Roaming:
                //Ask AI to calculate new move position
                if (Vector2.Distance(movePos, this.transform.position) < 0.01){
                    movePos = ai.NewRoamPos(this.transform.position, roomCenter);
                    MovementScript.SetPositions(this.transform.position, movePos);
                }
                //If not at new movePos, move toward that position
                else{
                    MovementScript.MoveToward(movePos, ai.roamingSpeed, ref ai, ai.roamingCurve);
                }
                setState();
                break;


            case AIState.Aggressive:
                //Ask AI to calculate new move position
                timeSinceLastAction += Time.deltaTime;
                if (Vector2.Distance(movePos, this.transform.position) < 0.01){
                    if (timeSinceLastAction > ai.aggressiveDelay){
                        movePos = ai.NewAggressivePos(this.transform.position, target.position, roomCenter);
                        MovementScript.SetPositions(this.transform.position, movePos);
                        timeSinceLastAction = 0;
                    }
                }
                //if not at new movePos, move toward that position
                else{
                    MovementScript.MoveToward(movePos, ai.aggressiveSpeed, ref ai, ai.aggressiveCurve);
                }
                //Check if conditions are correct to shoot, if so, calculate an aiming position and shoot
                if (ai.isReadyToShoot(this.transform.position, target)){
                    state = AIState.Shooting;
                    return;
                }
                setState();
                break;

            case AIState.Shooting:
                //shoot, then return to previous state
                Debug.Log("PRESSING SHOOT BUTTON");
                CombatScript.UseMainHand(ai.Aim(this.transform.position, target.position));
                //ADD: once weapon usage is figured out, implement it here. Should pass the ai position into the weapon use
                state = AIState.Aggressive;//defaulting to aggressive, FOR NOW
                break;

            case AIState.Retreating:
                timeSinceLastAction += Time.deltaTime;
                if (Vector2.Distance(movePos, this.transform.position) < 0.5){
                    if (timeSinceLastAction > ai.retreatDelay){
                        movePos = ai.NewRetreatPos(this.transform.position, target.position, roomCenter);
                        MovementScript.SetPositions(this.transform.position, movePos);
                        timeSinceLastAction = 0;
                    }
                }
                //if not at new movePos, move toward that position
                else{
                    MovementScript.MoveToward(movePos, ai.retreatSpeed, ref ai, ai.retreatCurve);
                }
                //Check if conditions are correct to shoot, if so, calculate an aiming position and shoot
                setState(); //normally, will stay retreating unless somehow heals
                break;

        }
        
    }

    void setState(){
        target = ai.GetTarget(this.transform.position);
        if (target){ //if there is a detectable target, consider either to aggress or retreat
            float healthPercent = CombatScript.getStats().getHealth()/CombatScript.getStats().getMaxHealth();
            //Debug.Log("HEALTH PERCENT IS " + healthPercent.ToString());
            if (ai.shouldFlee(healthPercent)){
                //Debug.Log("SWITCHING TO RETREAT STATE");
                state = AIState.Retreating;
            }
            else{
                //Debug.Log("SWITCHING TO AGGRESSIVE STATE");
                state = AIState.Aggressive;
                }
        }
        else{
            //Debug.Log("SWITCHING TO ROAMING STATE");
            state = AIState.Roaming;
        }
    }

    public void setAlive(bool liveStatus)
    {
        isAlive = liveStatus;
    }

    public bool getAlive()
    {
        return isAlive;
    }

}
