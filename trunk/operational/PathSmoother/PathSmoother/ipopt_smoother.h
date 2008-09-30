#ifndef _IPOPT_SMOOTHER_H
#define _IPOPT_SMOOTHER_H

#ifdef __cplusplus_cli
#pragma managed(push,off)
#endif

#include "smoother.h"
#include "iptnlp.hpp"
#include "IpIpoptApplication.hpp"
#include "IpIpoptData.hpp"

using namespace Ipopt;

class ipopt_smoother : public smoother, public TNLP {
public:
	/** default constructor */
  ipopt_smoother(const shared_ptr<line_list>& base_path, const shared_ptr<bound_finder>& ub, const shared_ptr<bound_finder>& lb);

  /** default destructor */
  virtual ~ipopt_smoother();

  /**@name Overloaded from TNLP */
  //@{
  /** Method to return some info about the nlp */
  virtual bool get_nlp_info(Index& n, Index& m, Index& nnz_jac_g,
                            Index& nnz_h_lag, IndexStyleEnum& index_style);

  /** Method to return the bounds for my problem */
  virtual bool get_bounds_info(Index n, Number* x_l, Number* x_u,
                               Index m, Number* g_l, Number* g_u);

  /** Method to return the starting point for the algorithm */
  virtual bool get_starting_point(Index n, bool init_x, Number* x,
                                  bool init_z, Number* z_L, Number* z_U,
                                  Index m, bool init_lambda,
                                  Number* lambda);

	virtual bool get_variables_linearity(Index n, LinearityType* var_types);
	virtual bool get_constraints_linearity(Index m, LinearityType* const_types);

  /** Method to return the objective value */
  virtual bool eval_f(Index n, const Number* x, bool new_x, Number& obj_value);

  /** Method to return the gradient of the objective */
  virtual bool eval_grad_f(Index n, const Number* x, bool new_x, Number* grad_f);

  /** Method to return the constraint residuals */
  virtual bool eval_g(Index n, const Number* x, bool new_x, Index m, Number* g);

  /** Method to return:
   *   1) The structure of the jacobian (if "values" is NULL)
   *   2) The values of the jacobian (if "values" is not NULL)
   */
  virtual bool eval_jac_g(Index n, const Number* x, bool new_x,
                          Index m, Index nele_jac, Index* iRow, Index *jCol,
                          Number* values);

  /** Method to return:
   *   1) The structure of the hessian of the lagrangian (if "values" is NULL)
   *   2) The values of the hessian of the lagrangian (if "values" is not NULL)
   */
  virtual bool eval_h(Index n, const Number* x, bool new_x,
                      Number obj_factor, Index m, const Number* lambda,
                      bool new_lambda, Index nele_hess, Index* iRow,
                      Index* jCol, Number* values);

	// evaluation routine called by base smoother class
	virtual solve_result smooth_iter(double mu0, double* w, bool w0, double* y, bool y0, double* z_l, double* z_u, bool z0);

  //@}

  /** @name Solution Methods */
  //@{
  /** This method is called when the algorithm is complete so the TNLP can store/write the solution */
  virtual void finalize_solution(SolverReturn status,
                                 Index n, const Number* x, const Number* z_L, const Number* z_U,
                                 Index m, const Number* g, const Number* lambda,
                                 Number obj_value,
				 const IpoptData* ip_data,
				 IpoptCalculatedQuantities* ip_cq);
  //@}

	virtual bool intermediate_callback(AlgorithmMode mode,
                                       Index iter, Number obj_value,
                                       Number inf_pr, Number inf_du,
                                       Number mu, Number d_norm,
                                       Number regularization_size,
                                       Number alpha_du, Number alpha_pr,
                                       Index ls_trials,
                                       const IpoptData* ip_data,
                                       IpoptCalculatedQuantities* ip_cq);

private:
	double* w;
	bool has_w0;
	
	double* z_l;
	double* z_u;
	bool has_z0;

	double* lambda;
	bool has_lambda0;

	timestamp start_time;

	IpoptApplication* app;
	SmartPtr<TNLP> this_ptr;

  /**@name Methods to block default compiler methods.
   * The compiler automatically generates the following three methods.
   *  Since the default compiler implementation is generally not what
   *  you want (for all but the most simple classes), we usually 
   *  put the declarations of these methods in the private section
   *  and never implement them. This prevents the compiler from
   *  implementing an incorrect "default" behavior without us
   *  knowing. (See Scott Meyers book, "Effective C++")
   *  
   */
  //@{
  //  HS071_NLP();
  ipopt_smoother(const ipopt_smoother&);
  ipopt_smoother& operator=(const ipopt_smoother&);
  //@}
};

#ifdef __cplusplus_cli
#pragma managed(pop)
#endif

#endif