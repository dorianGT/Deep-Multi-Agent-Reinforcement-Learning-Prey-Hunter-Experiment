using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent repr�sentant une proie dans l�environnement.
/// Se d�place pour �chapper aux chasseurs et maximise sa survie.
/// </summary>
public class PreyAgent : Agent
{
    /// <summary>
    /// Vitesse de d�placement vers l�avant.
    /// </summary>
    public float moveSpeed = 3f;

    /// <summary>
    /// Vitesse de rotation autour de l�axe Y.
    /// </summary>
    public float rotationSpeed = 200f;

    /// <summary>
    /// R�f�rence vers l�environnement global.
    /// </summary>
    private HunterPreyEnv env;

    /// <summary>
    /// Associe l�environnement � cet agent.
    /// </summary>
    /// <param name="e">L�environnement principal HunterPreyEnv.</param>
    public void SetEnv(HunterPreyEnv e) => env = e;

    /// <summary>
    /// R�initialisation de l�agent au d�but d�un �pisode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Peut �tre compl�t� si besoin : repositionner, reset timer, etc.
    }

    /// <summary>
    /// Collecte les observations de l�environnement (� compl�ter).
    /// Permet � l�agent de percevoir les �l�ments autour.
    /// </summary>
    /// <param name="sensor">Le capteur d�observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // � compl�ter : distances aux chasseurs, raycasts, murs proches, etc.
    }

    /// <summary>
    /// Re�oit et applique les actions continues.
    /// La proie peut tourner et avancer.
    /// Une petite r�compense est donn�e � chaque frame pour survivre plus longtemps.
    /// </summary>
    /// <param name="actions">Actions continues : [rotation, mouvement].</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        float rotation = actions.ContinuousActions[0];
        float forward = actions.ContinuousActions[1]; 

        // Appliquer la rotation
        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime);

        // Appliquer l'avance
        transform.position += transform.forward * forward * moveSpeed * Time.deltaTime;

        // R�compense pour avoir surv�cu un pas de temps
        AddReward(0.01f);
    }
}
