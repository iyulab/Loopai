"""
로컬 개발용 테스트 아티팩트 준비 스크립트.

로컬에서 Edge Runtime을 실행하기 위한 샘플 프로그램 아티팩트를 생성합니다.
"""

import sys
from pathlib import Path
from uuid import uuid4

# 프로젝트 루트를 Python path에 추가
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root / "src"))

from loopai.models import (
    ComplexityMetrics,
    ProgramArtifact,
    ProgramStatus,
    SynthesisStrategy,
)
from loopai.runtime.artifact_cache import ArtifactCache


def create_spam_classifier_v1():
    """간단한 스팸 분류기 v1 (buy만 탐지)."""
    return ProgramArtifact(
        id=uuid4(),
        task_id=uuid4(),
        version=1,
        language="python",
        code='''def classify(text: str) -> str:
    """간단한 스팸 분류기 - 'buy'가 포함되면 spam."""
    return "spam" if "buy" in text.lower() else "ham"''',
        synthesis_strategy=SynthesisStrategy.RULE,
        confidence_score=0.90,
        complexity_metrics=ComplexityMetrics(
            lines_of_code=3,
            cyclomatic_complexity=2,
            estimated_latency_ms=0.5,
        ),
        llm_provider="openai",
        llm_model="gpt-4",
        generation_cost=0.15,
        generation_time_sec=8.5,
        status=ProgramStatus.VALIDATED,
        deployment_percentage=0.0,
    )


def create_spam_classifier_v2():
    """개선된 스팸 분류기 v2 (여러 키워드 탐지)."""
    return ProgramArtifact(
        id=uuid4(),
        task_id=uuid4(),
        version=2,
        language="python",
        code='''def classify(text: str) -> str:
    """개선된 스팸 분류기 - 여러 스팸 키워드 탐지."""
    spam_keywords = ["buy", "free", "winner", "click", "prize", "urgent"]
    text_lower = text.lower()

    # 스팸 키워드가 2개 이상이면 확실히 spam
    spam_count = sum(1 for keyword in spam_keywords if keyword in text_lower)

    return "spam" if spam_count >= 1 else "ham"''',
        synthesis_strategy=SynthesisStrategy.RULE,
        confidence_score=0.95,
        complexity_metrics=ComplexityMetrics(
            lines_of_code=8,
            cyclomatic_complexity=3,
            estimated_latency_ms=1.0,
        ),
        llm_provider="openai",
        llm_model="gpt-4",
        generation_cost=0.20,
        generation_time_sec=10.2,
        status=ProgramStatus.VALIDATED,
        deployment_percentage=0.0,
    )


def main():
    """테스트 아티팩트 생성 및 저장."""
    print("🚀 로컬 개발용 테스트 아티팩트 준비 중...")
    print()

    # 데이터 디렉토리 및 task ID
    data_dir = project_root / "loopai-data"
    task_id = "test-task"

    # ArtifactCache 초기화
    cache = ArtifactCache(data_dir=str(data_dir))
    print(f"📁 데이터 디렉토리: {data_dir}")
    print(f"🏷️  Task ID: {task_id}")
    print()

    # 아티팩트 생성
    artifact_v1 = create_spam_classifier_v1()
    artifact_v2 = create_spam_classifier_v2()

    # 저장
    print("💾 아티팩트 v1 저장 중...")
    cache.store_artifact(task_id, artifact_v1)
    print("   ✅ v1 저장 완료")

    print("💾 아티팩트 v2 저장 중...")
    cache.store_artifact(task_id, artifact_v2)
    print("   ✅ v2 저장 완료")
    print()

    # 활성 버전 설정 (기본: v2)
    print("🔧 활성 버전을 v2로 설정 중...")
    cache.set_active_version(task_id, version=2)
    print("   ✅ 활성 버전: v2")
    print()

    # 확인
    versions = cache.list_versions(task_id)
    active = cache.get_active_artifact(task_id)

    print("📊 저장된 아티팩트:")
    print(f"   버전 목록: {versions}")
    print(f"   활성 버전: v{active.version}")
    print(f"   신뢰도 점수: {active.confidence_score}")
    print(f"   코드 라인 수: {active.complexity_metrics.lines_of_code}")
    print()

    print("✅ 테스트 아티팩트 준비 완료!")
    print()
    print("다음 명령으로 Edge Runtime 실행:")
    print("  python -m uvicorn loopai.runtime.main:app --reload --port 8080")
    print()
    print("API 테스트:")
    print('  curl http://localhost:8080/health')
    print('  curl -X POST http://localhost:8080/execute \\')
    print('    -H "Content-Type: application/json" \\')
    print('    -d \'{"input": {"text": "Buy now!"}}\'')


if __name__ == "__main__":
    main()
