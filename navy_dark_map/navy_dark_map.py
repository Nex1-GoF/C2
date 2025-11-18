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
# ============================================
def apply_dark_overlay(img: Image.Image, opacity=0.47):
    """
    OSM 타일 위에 네이비색 반투명 오버레이를 씌워
    다크모드 느낌을 만드는 함수
    """
    base = img.convert("RGBA")
    # 네이비 계열 컬러 (0, 20, 40)
    navy = Image.new("RGBA", base.size, (0, 20, 40, int(255 * opacity)))
    blended = Image.alpha_composite(base, navy)
    return blended.convert("RGB")


# ============================================
#   타일 제공 엔드포인트
# ============================================
@app.get("/tiles/{z}/{x}/{y}.png")
async def get_tile(z: int, x: int, y: int):

    cache_path = f"{TILE_CACHE_DIR}/{z}_{x}_{y}.png"

    # 1) 캐시가 있으면 즉시 반환
    if os.path.exists(cache_path):
        try:
            with open(cache_path, "rb") as f:
                return Response(content=f.read(), media_type="image/png")
        except:
            pass  # 읽기 실패하면 계속 진행

    # 2) 원본 OSM 타일 다운로드 시도
    url = OSM_BASE.format(z=z, x=x, y=y)
    try:
        resp = await client.get(url)
    except Exception:
        # 네트워크 에러 → 그냥 404
        return Response(status_code=404)

    # 3) HTTP 상태코드 체크
    if resp.status_code != 200:
        return Response(status_code=404)

    # 4) PNG MIME 타입인지 검사 (HTML 내려오면 바로 차단)
    content_type = resp.headers.get("Content-Type", "")
    if "image" not in content_type:
        return Response(status_code=404)

    # 5) PIL로 이미지 열기 시도
    try:
        img = Image.open(BytesIO(resp.content))
    except Exception:
        # 이미지 깨져있으면 캐시 저장 안 하고 404 반환
        return Response(status_code=404)

    # 6) 다크모드 변환 적용
    dark_img = apply_dark_overlay(img)

    # 7) 변환된 PNG를 파일 기반 캐시에 저장
    try:
        buf = BytesIO()
        dark_img.save(buf, format="PNG")
        data = buf.getvalue()

        with open(cache_path, "wb") as f:
            f.write(data)

    except Exception:
        # 저장 실패해도 일단 이미지는 반환함
        data = buf.getvalue()

    # 8) 최종 반환
    return Response(content=data, media_type="image/png")
