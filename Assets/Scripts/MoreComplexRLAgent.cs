using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent d'apprentissage par renforcement (RL) utilisant ML-Agents.
/// L'objectif est d'atteindre une cible en restant dans la zone.
/// </summary>
public class MoreComplexRLAgent : Agent
{

    #region Variables

    /// <summary>
    /// Transform de la cible à atteindre.
    /// </summary>
    public Transform target;

    /// <summary>
    /// Position de base pour le spawn de la target.
    /// </summary>
    public Transform spawnPos;

    /// <summary>
    /// Active ou désactive le placement aléatoire de la target.
    /// </summary>
    public bool randomSpawn = true;

    /// <summary>
    /// Rayon autour du point de spawn dans lequel la cible peut apparaître.
    /// </summary>
    public float randomRange = 3f;

    /// <summary>
    /// Vitesse de déplacement de l'agent.
    /// </summary>
    public float moveSpeed = 5f;

    /// <summary>
    /// Objet qui change de couleur pour donner un feedback visuel (victoire/défaite).
    /// </summary>
    public GameObject feedbackObject;

    /// <summary>
    /// Matériau affiché en cas de succès (l'agent touche la cible).
    /// </summary>
    public Material winMaterial;

    /// <summary>
    /// Matériau affiché en cas d'échec (l'agent touche un mur).
    /// </summary>
    public Material loseMaterial;

    /// <summary>
    /// Position de départ de l'agent.
    /// </summary>
    private Vector3 startPosition;

    /// <summary>
    /// Position de départ de la cible.
    /// </summary>
    private Vector3 targetStartPosition;

    /// <summary>
    /// Derniere distance par rapport a la target
    /// </summary>
    private float lastDistanceToTarget = -1f;

    #endregion

    #region Monobehaviour Functions

    /// <summary>
    /// Cette fonction est appelé au debut une seule fois, c'est une methode de base de unity.
    /// Initialisation des positions de départ.
    /// </summary>
    private void Start()
    {
        startPosition = transform.position;

        targetStartPosition = target.position;

        if (spawnPos == null)
        {
            spawnPos = target; // fallback si jamais rien n'est assigné
        }

    }

    #endregion

    #region Agent RL Functions

    /// <summary>
    /// Fonction appelée au début de chaque épisode.
    /// Permet de reset les positions de l'agent et de la cible.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        transform.position = startPosition;

        // Placement aléatoire de la target dans une zone définie
        if (randomSpawn && spawnPos != null)
        {
            target.position = spawnPos.position + new Vector3(
                Random.Range(-randomRange, randomRange),
                0,
                Random.Range(-randomRange, randomRange)
            );
        }
        else
        {
            target.position = spawnPos != null ? spawnPos.position : targetStartPosition;
        }

        lastDistanceToTarget = Vector3.Distance(transform.position, target.position);
    }

    /// <summary>
    /// Fonction qui définit les informations observées par l'agent.
    /// Ici : position de l'agent + position de la target.
    /// </summary>
    /// <param name="sensor">Objet permettant d'enregistrer les observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(target.position);
    }

    /// <summary>
    /// Fonction appelée quand ML-Agents décide des actions de l'agent.
    /// Ici : se déplacer sur l'axe X et Z.
    /// </summary>
    /// <param name="actions">Actions à effectuer.</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 move = new Vector3(moveX, 0, moveZ);
        transform.position += moveSpeed * Time.deltaTime * move;

        // --- Récompense basée sur la distance ---
        float currentDistance = Vector3.Distance(transform.position, target.position);

        // Récompense s'il s'approche de la cible
        if (lastDistanceToTarget > 0f)
        {
            float delta = lastDistanceToTarget - currentDistance;
            // plus il se rapproche, plus il est récompensé
            if (delta > 0f)
            {
                AddReward(delta);
            }
            else
            {
                AddReward(delta * 2);
            }
        }

        lastDistanceToTarget = currentDistance;

        // --- Pénalité à chaque étape (encourage à finir vite) ---
        AddReward(-0.001f);
    }

    /// <summary>
    /// Fonction utile pour tester l'agent manuellement (inputs clavier).
    /// </summary>
    /// <param name="actionsOut">Actions à envoyer à l'agent.</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    #endregion

    #region Trigger & Visuals

    /// <summary>
    /// Fonction appelée lors d'une collision avec un trigger.
    /// Gère les récompenses et la fin d'épisode.
    /// </summary>
    /// <param name="other">Objet avec lequel l'agent entre en collision.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(1.0f);               // Récompense
            SetFeedbackMaterial(winMaterial); // Visual feedback
            EndEpisode();                  // Fin de l'épisode
        }
        else if (other.CompareTag("Wall"))
        {
            SetReward(-1.0f);              // Punition
            SetFeedbackMaterial(loseMaterial); // Visual feedback
            EndEpisode();                  // Fin de l'épisode
        }
    }

    /// <summary>
    /// Change le matériau de l'objet de feedback visuel.
    /// </summary>
    /// <param name="mat">Matériau à appliquer.</param>
    private void SetFeedbackMaterial(Material mat)
    {
        if (feedbackObject != null && mat != null)
        {
            feedbackObject.GetComponent<Renderer>().material = mat;
        }
    }

    #endregion
}
