"""Build a deterministic copy/literal patch; inputs are local official and audited EXEs."""
import gzip, hashlib, json, struct, sys
from pathlib import Path

ORIGINAL = '6b6e1260cb5a1d6a303855f1c60cab3d0bc1aebba7e40a964180188b271e7386'
PATCHED = '6be10780d1f1471bb5bddefdfbcb3966dbc90cc2f00d2dbce579fa75190932b9'

def make(source, target):
    index = {}
    for p in range(0, len(source)-16, 64):
        index.setdefault(source[p:p+16], p)
    operations, literal = [], bytearray()
    p = 0
    while p < len(target):
        q = index.get(target[p:p+16]) if p+16 <= len(target) else None
        if q is None:
            literal.append(target[p]); p += 1; continue
        n = 16
        while p+n+4096 <= len(target) and q+n+4096 <= len(source) and target[p+n:p+n+4096] == source[q+n:q+n+4096]:
            n += 4096
        while p+n < len(target) and q+n < len(source) and target[p+n] == source[q+n]:
            n += 1
        if literal:
            operations.append((1, bytes(literal))); literal.clear()
        operations.append((0, q, n)); p += n
    if literal: operations.append((1, bytes(literal)))
    data = bytearray(b'PCLR2991' + bytes.fromhex(ORIGINAL) + bytes.fromhex(PATCHED) + struct.pack('<ii',len(target),len(operations)))
    rebuilt = bytearray()
    for op in operations:
        data.append(op[0])
        if op[0] == 0:
            data += struct.pack('<ii',op[1],op[2]); rebuilt += source[op[1]:op[1]+op[2]]
        else:
            data += struct.pack('<i',len(op[1])) + op[1]; rebuilt += op[1]
    assert rebuilt == target
    return gzip.compress(data, mtime=0), len(operations)

if __name__ == '__main__':
    source, target = (Path(x).read_bytes() for x in sys.argv[1:3])
    assert hashlib.sha256(source).hexdigest() == ORIGINAL
    assert hashlib.sha256(target).hexdigest() == PATCHED
    patch, count = make(source, target)
    root = Path(__file__).resolve().parent
    (root/'resources'/'restore.patch.gz').write_bytes(patch)
    manifest = {'format':'PCLR2991+gzip','source_sha256':ORIGINAL,'target_sha256':PATCHED,'target_length':len(target),'patch_sha256':hashlib.sha256(patch).hexdigest(),'patch_length':len(patch),'operations':count}
    (root/'resources'/'patch-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(manifest))
