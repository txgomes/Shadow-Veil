#include "ShadowVeilPawn.h"
#include "Camera/CameraComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "GameFramework/SpringArmComponent.h"

AShadowVeilPawn::AShadowVeilPawn()
{
	PrimaryActorTick.bCanEverTick = true;
	Body = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("HeroBody"));
	static ConstructorHelpers::FObjectFinder<UStaticMesh> MeshAsset(TEXT("/Engine/BasicShapes/Capsule.Capsule"));
	if (MeshAsset.Succeeded()) Body->SetStaticMesh(MeshAsset.Object);
	Body->SetCollisionProfileName(TEXT("Pawn"));
	Body->SetRelativeScale3D(FVector(0.62f, 0.62f, 1.05f));
	RootComponent = Body;

	CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
	CameraBoom->SetupAttachment(RootComponent);
	CameraBoom->TargetArmLength = 1120.0f;
	CameraBoom->SetRelativeRotation(FRotator(-58.0f, -90.0f, 0.0f));
	CameraBoom->bDoCollisionTest = false;
	CameraBoom->bEnableCameraLag = true;
	CameraBoom->CameraLagSpeed = 5.5f;

	FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
	FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
	FollowCamera->SetFieldOfView(42.0f);
	AutoPossessPlayer = EAutoReceiveInput::Player0;
}

void AShadowVeilPawn::SetMoveDestination(const FVector& WorldPosition)
{
	Destination = FVector(WorldPosition.X, WorldPosition.Y, GetActorLocation().Z);
	bHasDestination = FVector::Dist2D(GetActorLocation(), Destination) > 18.0f;
}

void AShadowVeilPawn::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	if (!bHasDestination) return;

	const FVector Current = GetActorLocation();
	const FVector Delta = Destination - Current;
	const float Distance = Delta.Size2D();
	if (Distance < 14.0f)
	{
		bHasDestination = false;
		return;
	}

	const FVector Direction = FVector(Delta.X, Delta.Y, 0.0f).GetSafeNormal();
	const float Step = FMath::Min(MoveSpeed * DeltaSeconds, Distance);
	SetActorLocation(Current + Direction * Step, true);
	const FRotator DesiredRotation = Direction.Rotation();
	SetActorRotation(FMath::RInterpConstantTo(GetActorRotation(), DesiredRotation, DeltaSeconds, TurnSpeed));
}
