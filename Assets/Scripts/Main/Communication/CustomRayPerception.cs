using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// G�re un syst�me de raycasts personnalis�s autour de l'agent.
/// Permet d'observer les objets d�tectables dans le champ de vision.
/// </summary>
public class CustomRayPerception : MonoBehaviour
{
    [Header("D�tection")]
    public List<string> detectableTags = new List<string>();

    [Header("Configuration des Rayons")]
    public int raysPerDirection = 3;
    public float maxRayDegrees = 60f;
    public float sphereCastRadius = 0.0f;
    public float rayLength = 10f;
    public LayerMask rayLayerMask;

    [Header("Offsets Verticaux")]
    public float startVerticalOffset = 0.5f;
    public float endVerticalOffset = 0.5f;

    [Header("Options Diverses")]
    public bool alternatingRayOrder = true;
    public bool debugGizmos = true;
    public Color rayHitColor = Color.red;
    public Color rayMissColor = Color.green;

    /// <summary>
    /// R�sultat d�un raycast : tag d�tect�, distance, direction, etc.
    /// </summary>
    private struct RayResult
    {
        public string tag;
        public float distance;             // Distance r�elle
        public float normalizedDistance;   // Distance entre 0 et 1
        public Vector3 point;
        public Vector3 direction;
    }

    /// <summary>
    /// Lance les rayons et collecte les r�sultats des collisions.
    /// </summary>
    /// <returns>Liste des r�sultats des rayons.</returns>
    private List<RayResult> PerformRaycasts()
    {
        List<RayResult> results = new List<RayResult>();
        Vector3 startPosition = transform.position + Vector3.up * startVerticalOffset;

        foreach (Vector3 dir in GenerateRayDirections())
        {
            Vector3 endPosition = transform.position + Vector3.up * endVerticalOffset + dir * rayLength;
            Vector3 rayDirection = (endPosition - startPosition).normalized;
            Ray ray = new Ray(startPosition, rayDirection);
            RaycastHit hit;
            bool hasHit;

            if (sphereCastRadius > 0f)
            {
                hasHit = Physics.SphereCast(ray, sphereCastRadius, out hit, rayLength, rayLayerMask);
            }
            else
            {
                hasHit = Physics.Raycast(ray, out hit, rayLength, rayLayerMask);
            }

            if (hasHit && detectableTags.Contains(hit.collider.tag))
            {
                results.Add(new RayResult
                {
                    tag = hit.collider.tag,
                    distance = hit.distance,
                    normalizedDistance = hit.distance / rayLength,
                    point = hit.point,
                    direction = rayDirection
                });
            }
            else
            {
                results.Add(new RayResult
                {
                    tag = "", // Aucun objet d�tect�
                    distance = rayLength,
                    normalizedDistance = 1f, // Normalis�
                    point = startPosition + rayDirection * rayLength,
                    direction = rayDirection
                });
            }

        }

        return results;
    }

    /// <summary>
    /// G�n�re un tableau de float repr�sentant les tags d�tect�s et les distances normalis�es.
    /// </summary>
    /// <param name="rayResults">R�sultats des rayons.</param>
    /// <param name="detectableTags">Liste des tags � d�tecter.</param>
    /// <returns>Observation encod�e pour l'agent.</returns>
    private float[] GetObservationFromRayResults(List<RayResult> rayResults, List<string> detectableTags)
    {
        int tagCount = detectableTags.Count;
        int observationSize = (tagCount + 1) * rayResults.Count;
        float[] observation = new float[observationSize];

        for (int i = 0; i < rayResults.Count; i++)
        {
            RayResult result = rayResults[i];
            int baseIndex = i * (tagCount + 1);

            // One-hot encoding
            for (int j = 0; j < tagCount; j++)
            {
                observation[baseIndex + j] = (result.tag == detectableTags[j]) ? 1f : 0f;
            }

            // Normalized distance
            observation[baseIndex + tagCount] = result.normalizedDistance;
        }

        return observation;
    }

    /// <summary>
    /// Retourne deux types d�observations pour l�agent (tags + distance).
    /// </summary>
    /// <param name="tags">Cat�gories � observer.</param>
    /// <returns>Double tableau de float : une observation compl�te et une filtr�e.</returns>
    public float[][] GetObservations(List<string> tags)
    {
        List<RayResult> tmp = PerformRaycasts();
        return new float[][]
        {
        GetObservationFromRayResults(tmp, detectableTags),
        GetObservationOnly(tmp, tags)
        };
    }

    /// <summary>
    /// G�n�re les directions des rayons � partir de la configuration.
    /// </summary>
    /// <returns>Liste de directions.</returns>
    private List<Vector3> GenerateRayDirections()
    {
        List<Vector3> directions = new List<Vector3>();
        List<float> angles = new List<float> { 0f };

        for (int i = 1; i <= raysPerDirection; i++)
        {
            float angle = maxRayDegrees * i / raysPerDirection;
            if (alternatingRayOrder)
            {
                angles.Add(-angle);
                angles.Add(angle);
            }
            else
            {
                angles.Add(angle);
                angles.Add(-angle);
            }
        }

        foreach (float angle in angles)
        {
            directions.Add(Quaternion.Euler(0, angle, 0) * transform.forward);
        }

        return directions;
    }

    /// <summary>
    /// Encode uniquement les r�sultats des rayons pour certaines cat�gories de tags.
    /// </summary>
    /// <param name="rayResults">R�sultats des rayons.</param>
    /// <param name="tagCategories">Tags � encoder.</param>
    /// <returns>Observation filtr�e sous forme de tableau de float.</returns>
    private float[] GetObservationOnly(List<RayResult> rayResults, List<string> tagCategories)
    {
        int tagCount = tagCategories.Count;
        int observationSize = rayResults.Count * (tagCount + 1); // +1 for distance
        float[] observation = new float[observationSize];

        for (int i = 0; i < rayResults.Count; i++)
        {
            RayResult result = rayResults[i];
            int baseIndex = i * (tagCount + 1);

            // One-hot encoding
            for (int j = 0; j < tagCount; j++)
            {
                observation[baseIndex + j] = (result.tag == tagCategories[j]) ? 1f : 0f;
            }

            // Normalized distance
            observation[baseIndex + tagCount] = result.normalizedDistance;
        }

        return observation;
    }





    /// <summary>
    /// Dessine les rayons dans la sc�ne Unity pour le debug visuel.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;

        Vector3 startPosition = transform.position + Vector3.up * startVerticalOffset;

        foreach (Vector3 dir in GenerateRayDirections())
        {
            Vector3 endPosition = transform.position + Vector3.up * endVerticalOffset + dir * rayLength;
            Vector3 rayDirection = (endPosition - startPosition).normalized;
            Ray ray = new Ray(startPosition, rayDirection);
            RaycastHit hit;
            bool hasHit;

            if (sphereCastRadius > 0f)
            {
                hasHit = Physics.SphereCast(ray, sphereCastRadius, out hit, rayLength, rayLayerMask);
            }
            else
            {
                hasHit = Physics.Raycast(ray, out hit, rayLength, rayLayerMask);
            }

            if (hasHit && detectableTags.Contains(hit.collider.tag))
            {
                Gizmos.color = rayHitColor;
                Gizmos.DrawLine(startPosition, hit.point);
                Gizmos.DrawSphere(hit.point, sphereCastRadius);
            }
            else
            {
                Vector3 end = startPosition + rayDirection * rayLength;
                Gizmos.color = rayMissColor;
                Gizmos.DrawLine(startPosition, end);
                Gizmos.DrawSphere(end, sphereCastRadius);
            }
        }
    }

}
