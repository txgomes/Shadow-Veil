#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "ShadowVeilGameMode.generated.h"

UCLASS()
class SHADOWVEIL_API AShadowVeilGameMode : public AGameModeBase
{
	GENERATED_BODY()

public:
	AShadowVeilGameMode();
	virtual void BeginPlay() override;

private:
	void SpawnPrimitive(const TCHAR* MeshPath, const FVector& Location, const FVector& Scale, const FRotator& Rotation = FRotator::ZeroRotator);
};
