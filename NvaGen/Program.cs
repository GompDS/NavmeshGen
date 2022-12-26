using System.Numerics;
using SoulsFormats;

namespace NvaGen;

public class Program
{
    public static void Main(string[] args)
    {
        if (!NVA.IsRead(args.FirstOrDefault(x => NVA.Is(x), ""), out NVA nva))
        {
            throw new ArgumentException("No nva included in program arguments.");
        }

        NVA nvaBackup = NVA.Read(args.FirstOrDefault(x => NVA.Is(x)));
        
        if (!BND4.IsRead(args.FirstOrDefault(x => x.EndsWith("nvmhktbnd.dcx"), ""), out BND4 nvmhktbnd))
        {
            throw new ArgumentException("No nvmhktbnd included in program arguments.");
        }
        
        if (!MSB3.IsRead(args.FirstOrDefault(x => MSB3.Is(x), ""), out MSB3 msb))
        {
            throw new ArgumentException("No msb included in program arguments.");
        }

        foreach (BinderFile file in nvmhktbnd.Files.Where(x => 
                     nva.Navmeshes.All(y => y.ModelID != int.Parse(Path.GetFileName(x.Name)[13..19]))))
        {
            int area = int.Parse(Path.GetFileName(file.Name)[1..3]);
            int block = int.Parse(Path.GetFileName(file.Name)[4..6]);
            int model = int.Parse(Path.GetFileName(file.Name)[13..19]);
            AddNavMeshToNva(area, block, model, nva, msb, nvmhktbnd);
        }
        
        string nvaName = args.FirstOrDefault(x => NVA.Is(x), "");
        if (!File.Exists($"{nvaName}.bak"))
        {
            nvaBackup.Write($"{nvaName}.bak");
        }
        nva.Write($"{nvaName}");
    }

    private static void AddNavMeshToNva(int area, int block, int model, NVA nva, MSB3 msb, BND4 nvmhktbnd)
    {
        BinderFile? navBinderFile = nvmhktbnd.Files.FirstOrDefault(x => x.Name.EndsWith(model.ToString("D6") + ".hkx"));
        if (navBinderFile == null)
        {
            return;
        }

        BinaryReaderEx br = new (false, navBinderFile.Bytes);
        for (int i = 0; i < 0x308; i += 4)
        {
            br.ReadUInt32();
        }
        int triangleCount = (int) br.ReadUInt32();
        if (triangleCount == 1936671604)
        {
            for (int i = 0x30C; i < 0x368; i += 4)
            {
                br.ReadUInt32();
            }
            triangleCount = (int) br.ReadUInt32();
        }

        Vector3 m39Position = new Vector3((float)135.906, (float)-325.76, (float)-874.356);
        Vector3 m39Rotation = new Vector3(0, (float)-1.4189527, 0);
        

        foreach (MSB3.Part.Collision col in msb.Parts.Collisions.Where(x => int.Parse(x.ModelName[1..]) == model))
        {
            NVA.Navmesh navmesh = new()
            {
                MapNodeCount = -1,
                NameID = int.Parse($"{area}{block}{model:D6}"),
                ModelID = model,
                Position = new Vector3(),
                Rotation = new Vector3(),
                TriangleCount = triangleCount,
                Unk4C = false
            };
            for (int j = 0; j < navmesh.TriangleCount; j++) {
                nva.Entries1.Add(new NVA.Entry1());
            }
            nva.Navmeshes.Add(navmesh);
        }
    }
    
    private static void AddTempNavMeshToNVA(int block, int area, NVA nva) {

        /* Just add one Navmesh to each nva. Model and Name are not a string, so no '_0000' format, and we have to use a unique ID here. */
        int nModelID = 0;
        if (block == 8)
            nModelID = 91;

        if (int.TryParse($"{area}{block}{nModelID:D6}", out int id)) //This is just for testing so we don't go over int.MaxValue.
        {
            nva.Navmeshes.Add(new NVA.Navmesh() {
                NameID = id,
                ModelID = nModelID,
                Position = block == 3 ? new Vector3(798, 3, -185) : new Vector3(), //new Vector3(716, 2, -514),// player.Position, //using player position, here. Change this to cell.center in loop.
                TriangleCount = 1,
                //Unk38 = 12399,
                //Unk4C = true
            });
        }

        //WriteTestNavMesh(nModelID, area, block);

        /* There has to be an entry for each vertex in each navmesh in nav.Navmashes */
        foreach (NVA.Navmesh navmesh in nva.Navmeshes) {
            for (int j = 0; j < navmesh.TriangleCount; j++) {
                nva.Entries1.Add(new NVA.Entry1());
            }
        }
    }
}