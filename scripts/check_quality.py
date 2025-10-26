#!/usr/bin/env python
"""
코드 품질 체크 스크립트.

Black, Ruff, MyPy, pytest를 모두 실행하여 코드 품질을 검증합니다.
"""

import subprocess
import sys
from pathlib import Path

# 색상 코드
GREEN = "\033[92m"
RED = "\033[91m"
YELLOW = "\033[93m"
BLUE = "\033[94m"
RESET = "\033[0m"


def run_command(name: str, command: list) -> bool:
    """명령 실행 및 결과 출력."""
    print(f"\n{BLUE}{'='*60}{RESET}")
    print(f"{BLUE}🔍 {name}{RESET}")
    print(f"{BLUE}{'='*60}{RESET}\n")

    try:
        result = subprocess.run(
            command,
            capture_output=False,
            text=True,
            check=True,
        )
        print(f"\n{GREEN}✅ {name} 통과{RESET}")
        return True
    except subprocess.CalledProcessError as e:
        print(f"\n{RED}❌ {name} 실패{RESET}")
        return False


def main():
    """모든 품질 체크 실행."""
    print(f"\n{BLUE}{'='*60}{RESET}")
    print(f"{BLUE}🚀 Loopai 코드 품질 체크 시작{RESET}")
    print(f"{BLUE}{'='*60}{RESET}")

    checks = []

    # 1. Black (코드 포맷팅 체크)
    checks.append(
        run_command(
            "Black (포맷팅 체크)",
            ["black", "--check", "src/", "tests/", "scripts/"]
        )
    )

    # 2. Ruff (린팅)
    checks.append(
        run_command(
            "Ruff (린팅)",
            ["ruff", "check", "src/", "tests/", "scripts/"]
        )
    )

    # 3. MyPy (타입 체크)
    checks.append(
        run_command(
            "MyPy (타입 체크)",
            ["mypy", "src/loopai/"]
        )
    )

    # 4. pytest (테스트)
    checks.append(
        run_command(
            "pytest (Phase 3 테스트)",
            ["pytest", "tests/test_phase3_*.py", "-v", "--tb=short"]
        )
    )

    # 결과 요약
    print(f"\n{BLUE}{'='*60}{RESET}")
    print(f"{BLUE}📊 품질 체크 결과 요약{RESET}")
    print(f"{BLUE}{'='*60}{RESET}\n")

    passed = sum(checks)
    total = len(checks)

    print(f"총 {total}개 체크 중 {passed}개 통과")
    print()

    if passed == total:
        print(f"{GREEN}✅ 모든 품질 체크 통과!{RESET}")
        print(f"{GREEN}   코드를 커밋할 준비가 되었습니다.{RESET}")
        return 0
    else:
        failed = total - passed
        print(f"{RED}❌ {failed}개 체크 실패{RESET}")
        print(f"{YELLOW}   위의 문제를 해결한 후 다시 시도하세요.{RESET}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
