import optuna
import matplotlib.pyplot as plt
import seaborn as sns

def generate_academic_plot():
    # SQLite DB에서 Optuna 학습 기록 로드
    study_name = "arena_balance_study"
    storage_name = "sqlite:///arena_balance.db"
    
    try:
        study = optuna.load_study(study_name=study_name, storage=storage_name)
    except Exception as e:
        print(f"[오류] DB를 로드할 수 없습니다. 경로를 확인하십시오: {e}")
        return

    # 정상 완료된 Trial 데이터만 필터링하여 데이터프레임 변환
    df = study.trials_dataframe()
    df = df[df['state'] == 'COMPLETE']

    # 학술 논문용 시각화 스타일 설정
    sns.set_theme(style="whitegrid")
    plt.rcParams['font.family'] = 'serif'
    
    # 캔버스 해상도 및 크기 설정
    plt.figure(figsize=(10, 6), dpi=300)
    
    # 추세선 렌더링 (꺾은선 그래프)
    sns.lineplot(x=df['number'], y=df['value'], marker='o', color='#2c3e50', linewidth=2, markersize=6)

    # 최적의 황금 밸런스 지점 하이라이트 및 주석 달기
    best_trial_num = study.best_trial.number
    best_trial_val = study.best_trial.value

    plt.scatter(best_trial_num, best_trial_val, color='#e74c3c', s=150, zorder=5, label=f'Best Balance (Trial {best_trial_num})')
    
    plt.annotate(f'Global Minimum\nLoss: {best_trial_val:.4f}',
                 xy=(best_trial_num, best_trial_val),
                 xytext=(best_trial_num - 3, best_trial_val + 0.25),
                 arrowprops=dict(facecolor='black', shrink=0.05, width=1.5, headwidth=6),
                 fontsize=12, fontweight='bold', ha='center')

    # 축 및 타이틀 설정
    plt.title('Optimization of Game Balance via RL and Optuna', fontsize=16, fontweight='bold', pad=15)
    plt.xlabel('Trial Number', fontsize=14)
    plt.ylabel('Loss (Win Rate Gap + Tie Penalty)', fontsize=14)
    plt.xticks(fontsize=12)
    plt.yticks(fontsize=12)
    plt.legend(loc='upper right', fontsize=12)
    
    plt.tight_layout()

    # 논문 삽입용 고해상도 이미지 및 벡터 파일 추출
    plt.savefig('optimization_loss_trend.png', dpi=300)
    plt.savefig('optimization_loss_trend.pdf') # 화질 저하가 없는 PDF 포맷 병행 추출
    
    print("논문용 그래프 추출이 완료되었습니다. (optimization_loss_trend.png / pdf)")
    plt.show()

if __name__ == "__main__":
    generate_academic_plot()