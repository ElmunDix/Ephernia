using UnityEngine;

namespace MultiplayerARPG
{
    public class AllowToBuildOnSameAreaBelow : BaseBuildConditionForBuildingArea
    {
        public Vector3 raycastOriginOffsets = Vector3.up;
        public float raycastDistance = 10f;
        public LayerMask layerMask = 1;
        private RaycastHit[] raycastHits = new RaycastHit[32];

        public override bool AllowToBuild(BuildingArea sourceArea, BuildingEntity newBuilding)
        {
            int hitCount = PhysicUtils.SortedRaycastNonAlloc3D(sourceArea.transform.position + raycastOriginOffsets, Vector3.down, raycastHits, raycastDistance, layerMask.value);
            BuildingArea tempBuildingArea;
            for (int i = 0; i < hitCount; ++i)
            {
                if (raycastHits[i].transform == sourceArea.transform)
                    continue;
                tempBuildingArea = raycastHits[i].transform.GetComponent<BuildingArea>();
                if (tempBuildingArea != null && newBuilding.BuildingTypes.Contains(tempBuildingArea.buildingType))
                    return true;
            }
            return false;
        }
    }
}
