module go2cs/CollidingPackageNames

go 1.23

require (
	collidea/dup v0.0.0
	collideb/dup v0.0.0
)

replace collidea/dup => ./duprenamed

replace collideb/dup => ./dupplain
