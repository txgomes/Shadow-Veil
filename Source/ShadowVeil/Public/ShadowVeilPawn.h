#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "ShadowVeilPawn.generated.h"

class UCameraComponent;
class USpringArmComponent;
class UStaticMeshComponent;

UCLASS()
class SHADOWVEIL_API AShadowVeilPawn : public APawn
{
	GENERATED_BODY()

public:
	AShadowVeilPawn();
	virtual void Tick(float DeltaSeconds) override;
	void SetMoveDestination(const FVector& WorldPosition);
	bool IsMoving() const { return bHasDestination; }

protected:
	UPROPERTY(VisibleAnywhere, Category="Components") TObjectPtr<UStaticMeshComponent> Body;
	UPROPERTY(VisibleAnywhere, Category="Components") TObjectPtr<USpringArmComponent> CameraBoom;
	UPROPERTY(VisibleAnywhere, Category="Components") TObjectPtr<UCameraComponent> FollowCamera;
	UPROPERTY(EditAnywhere, Category="Movement") float MoveSpeed = 440.0f;
	UPROPERTY(EditAnywhere, Category="Movement") float TurnSpeed = 540.0f;

private:
	FVector Destination = FVector::ZeroVector;
	bool bHasDestination = false;
};
