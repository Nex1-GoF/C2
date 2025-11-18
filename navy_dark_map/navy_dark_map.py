from fastapi import FastAPI, Response
import httpx
from PIL import Image
from io import BytesIO
import os

app = FastAPI()

# ============================================
#   캐시 디렉토리
# ============================================
TILE_CACHE_DIR = "./cache"
os.makedirs(TILE_CACHE_DIR, exist_ok=True)

# ============================================
#   오픈스트리트맵 기본 타일 URL
# ============================================
OSM_BASE = "https://tile.openstreetmap.org/{z}/{x}/{y}.png"

# ============================================
#   HTTP 클라이언트 (재활용)
# ============================================
client = httpx.AsyncClient(
    headers={"User-Agent": "NavyDarkTileServer/1.0"},
    timeout=10
)

# ============================================
#   다크모드 오버레이 필터
#   (네이비 + 반투명 오버레이)
# ============================================
def apply_dark_overlay(img: Image.Image, opacity=0.47):
    """
    원래 OSM 타일 위에 네이비색 반투명 오버레이를 씌워서
    다크 테마로 변환하는 함수
    """
    base = img.convert("RGBA")

    # 원하는 네이비톤: (0, 20, 40)
    navy = Image.new("RGBA", base.size, (0, 20, 40, int(255 * opacity)))

    blended = Image.alpha_composite(base, navy)
    return blended.convert("RGB")


# ============================================
#   타일 제공 엔드포인트
# ============================================
@app.get("/tiles/{z}/{x}/{y}.png")
async def get_tile(z: int, x: int, y: int):

    cache_path = f"{TILE_CACHE_DIR}/{z}_{x}_{y}.png"

    # 1) 캐시 먼저 확인
    if os.path.exists(cache_path):
        with open(cache_path, "rb") as f:
            return Response(content=f.read(), media_type="image/png")

    # 2) OSM 타일 다운로드
    url = OSM_BASE.format(z=z, x=x, y=y)
    resp = await client.get(url)

    if resp.status_code != 200:
        return Response(status_code=404)

    # 3) 이미지 로딩
    try:
        img = Image.open(BytesIO(resp.content))
    except Exception:
        return Response(status_code=500)

    # 4) 다크모드 적용
    dark_img = apply_dark_overlay(img)

    # 5) 캐시 저장
    buf = BytesIO()
    dark_img.save(buf, format="PNG")
    data = buf.getvalue()

    with open(cache_path, "wb") as f:
        f.write(data)

    # 6) 반환
    return Response(content=data, media_type="image/png")
